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

output_path = f"{model_dir}output/"
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
faqs = [faq["matched_questions"] for faq in faq_config["faqs"]]
faq_count = sum(len(faq) for faq in faqs)

# Set up the dataset by pairing each question with another one
print("Generating datasets...")
# Training dataset store
sentences1 = []
sentences2 = []
scores = []

# Eval dataset stores
eval_sentences1 = []
eval_sentences2 = []
eval_scores = []

# Calculated eval params
eval_interval = sys.maxsize if eval_percent <= 0 else math.ceil(
    1 / eval_percent)
exclusive_eval_count = 0
eval_count = 0

# Collect data pairs
for faq_set in faqs:
    for faq in faq_set:
        is_exclusive_eval = not include_eval_in_other_pairs and exclusive_eval_count % eval_interval == 0
        exclusive_eval_count += 1
        exclusive_eval_other_count = 0

        for other_faq_set in faqs:
            for other_faq in other_faq_set:
                is_exclusive_eval_other = not include_eval_in_other_pairs and exclusive_eval_other_count % eval_interval == 0
                exclusive_eval_other_count += 1

                # Of course they're equal if they're the same thing, skip it
                # Or if it's mixing eval with non-eval, skip it
                if faq == other_faq or (is_exclusive_eval != is_exclusive_eval_other):
                    continue

                is_eval = eval_mode and include_eval_in_other_pairs and eval_count % eval_interval == 0
                eval_count += 1

                # If it's part of the same set, it's similar questions, otherwise it's not similar
                score = 1.0 if faq_set == other_faq_set else 0.0

                # Do not include any eval data in training data if it's exclusive
                if not is_exclusive_eval:
                    # Add to the training set if not eval
                    if not is_eval or include_eval_in_training:
                        sentences1.append(faq)
                        sentences2.append(other_faq)
                        scores.append(score)

                # Add to eval if it's within the interval
                if is_eval or is_exclusive_eval:
                    eval_sentences1.append(faq)
                    eval_sentences2.append(other_faq)
                    eval_scores.append(score)
print(
    f"Generated datasets: \n  > Training: {len(sentences1)} entries\n  > Evaluation: {len(eval_sentences1)} entries")

# Load the model to fine-tune
print("Loading model to fine-tune...")
model = SentenceTransformer(base_model, cache_folder=model_cache)

# Define the dataset, loss, training arguments, and evaluator
print("Preparing training...")
dataset = Dataset.from_dict({
    "sentence1": sentences1,
    "sentence2": sentences2,
    "score": scores,
})
if eval_mode:
    eval_dataset = Dataset.from_dict({
        "sentence1": eval_sentences1,
        "sentence2": eval_sentences2,
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
    fp16=True, # Set to False if you get an error that your GPU can't run on FP16
    bf16=False, # Set to True if you have a GPU that supports BF16
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
        sentences1=eval_sentences1,
        sentences2=eval_sentences2,
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
