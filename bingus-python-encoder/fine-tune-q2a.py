from datasets import Dataset
from sentence_transformers import SentenceTransformer, SentenceTransformerTrainer, SentenceTransformerTrainingArguments
from sentence_transformers.losses import CoSENTLoss
from sentence_transformers.evaluation import EmbeddingSimilarityEvaluator, SimilarityFunction

import sys
import os
import json
import math

# Define the default paths to check
faq_config_paths = ["./faq_config.json",
                    "../BingusApi/config/faq_config.json", "./BingusApi/config/faq_config.json"]

# Eval parameters
eval_mode = False
if eval_mode:
    include_eval_in_other_pairs = False
    include_eval_in_training = False
    eval_percent = 0.2
else:
    include_eval_in_other_pairs = True
    include_eval_in_training = True
    eval_percent = 0

# Model parameters
model_cache = "./model-cache/"
base_model = "all-MiniLM-L6-v2"

model_ver = 13
model_name = f"Bingus-v{model_ver}{'_Eval' if eval_mode else ''}_{base_model}"
model_dir = f"./local-models/{model_name}/"

output_path = f"{model_dir}{model_name}/"
checkpoint_path = f"{model_dir}checkpoints/"

# Search the paths for the config
faq_config = None
for cur_path in faq_config_paths:
    if os.path.isfile(cur_path):
        print(f"Found \"faq_config.json\" at \"{cur_path}\"!")
        print("Reading FAQ config...")
        with open(cur_path, "r") as f:
            faq_config = json.load(f)
        break
    else:
        print(f"Could not find \"faq_config.json\" at \"{cur_path}\".")

if faq_config is None:
    raise FileNotFoundError(
        "Could not find \"faq_config.json\" in any of the default paths.")

# Parse the config
# We use each FAQ's matched questions and the corresponding answer
faqs = faq_config["faqs"]
faq_count = len(faqs)

# Set up the dataset by pairing each question with the correct answer and incorrect answers
print("Generating datasets...")
# Training dataset store
questions = []
answers = []
scores = []

# Eval dataset stores
eval_questions = []
eval_answers = []
eval_scores = []

# Calculated eval params
eval_interval = sys.maxsize if eval_percent <= 0 else math.ceil(
    1 / eval_percent)
exclusive_eval_count = 0
eval_count = 0

# Collect data pairs
for faq in faqs:
    matched_questions = faq["matched_questions"]
    correct_answer = faq["answer"]

    is_exclusive_eval = not include_eval_in_other_pairs and exclusive_eval_count % eval_interval == 0
    exclusive_eval_count += 1
    exclusive_eval_other_count = 0

    # Pair each question with the correct answer
    for question in matched_questions:
        for other_faq in faqs:
            for other_question in other_faq["matched_questions"]:
                is_exclusive_eval_other = not include_eval_in_other_pairs and exclusive_eval_other_count % eval_interval == 0
                exclusive_eval_other_count += 1

                # Skip if it's the same question-answer pair or mixing eval with non-eval
                if question == other_question or (is_exclusive_eval != is_exclusive_eval_other):
                    continue

                is_eval = eval_mode and include_eval_in_other_pairs and eval_count % eval_interval == 0
                eval_count += 1

                # Score is 1.0 for correct answer, 0.0 for others
                score = 1.0 if correct_answer == other_faq["answer"] else 0.0

                # Do not include any eval data in training data if it's exclusive
                if not is_exclusive_eval:
                    # Add to the training set if not eval
                    if not is_eval or include_eval_in_training:
                        questions.append(question)
                        answers.append(other_faq["answer"])
                        scores.append(score)

                # Add to eval if it's within the interval
                if is_eval or is_exclusive_eval:
                    eval_questions.append(question)
                    eval_answers.append(other_faq["answer"])
                    eval_scores.append(score)

print(
    f"Generated datasets: \n  > Training: {len(questions)} entries\n  > Evaluation: {len(eval_questions)} entries")

# Load the model to fine-tune
print("Loading model to fine-tune...")
model = SentenceTransformer(base_model, cache_folder=model_cache)

# Define the dataset, loss, training arguments, and evaluator
print("Preparing training...")
dataset = Dataset.from_dict({
    "sentence1": questions,
    "sentence2": answers,
    "score": scores,
})
if eval_mode:
    eval_dataset = Dataset.from_dict({
        "sentence1": eval_questions,
        "sentence2": eval_answers,
        "score": eval_scores,
    })
else:
    eval_dataset = None

# CoSENTLoss - Slow to converge, doesn't overfit
# AnglELoss - Has trouble converging
# CosineSimilarityLoss - Overfits hard past 1 epoch
loss = CoSENTLoss(model)

args = SentenceTransformerTrainingArguments(
    # Required parameter:
    output_dir=checkpoint_path,
    # Optional training parameters:
    num_train_epochs=80,
    per_device_train_batch_size=128,
    per_device_eval_batch_size=128,
    learning_rate=0.00005 * math.sqrt(128/16),
    warmup_ratio=0.1,
    fp16=True,  # Set to False if you get an error that your GPU can't run on FP16
    bf16=False,  # Set to True if you have a GPU that supports BF16
    # Optional tracking/debugging parameters:
    eval_strategy="steps" if eval_mode else "no",
    eval_steps=2000 if eval_mode else 0,
    save_strategy="steps",
    save_steps=2000,
    save_total_limit=2,
    logging_steps=500,
    run_name=model_name,  # Will be used in W&B if `wandb` is installed
    do_eval=eval_mode,
    eval_on_start=eval_mode,
)

if eval_mode:
    dev_evaluator = EmbeddingSimilarityEvaluator(
        sentences1=eval_questions,
        sentences2=eval_answers,
        scores=eval_scores,
        main_similarity=SimilarityFunction.COSINE,
    )
else:
    dev_evaluator = None

# Tune the model
print("Fine-tuning the model...")
trainer = SentenceTransformerTrainer(
    model=model,
    args=args,
    train_dataset=dataset,
    eval_dataset=eval_dataset,
    loss=loss,
    evaluator=dev_evaluator,
)
trainer.train(resume_from_checkpoint=False)
model.save_pretrained(output_path)
