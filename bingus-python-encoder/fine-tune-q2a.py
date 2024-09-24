from datasets import Dataset
from sentence_transformers import SentenceTransformer, SentenceTransformerTrainer, SentenceTransformerTrainingArguments
from sentence_transformers.losses import CoSENTLoss
from sentence_transformers.evaluation import EmbeddingSimilarityEvaluator, SimilarityFunction
import os
import json
import math

# FAQ paths
faq_config_paths = [
    "./faq_config.json",
    "../BingusApi/config/faq_config.json",
    "./BingusApi/config/faq_config.json"
]

# Evaluation settings
eval_mode = False
eval_percent = 0.2 if eval_mode else 0

# Model settings
model_cache = "./model-cache/"
base_model = "all-MiniLM-L6-v2"
model_ver = 12
model_name = f"Bingus-v{model_ver}{'_Eval' if eval_mode else ''}_{base_model}"
model_dir = f"./local-models/{model_name}/"
output_path = f"{model_dir}{model_name}/"
checkpoint_path = f"{model_dir}checkpoints/"


def load_faq_config(paths):
    """
    Searches through a list of paths to find and load the first existing faq_config.json file.
    Raises a FileNotFoundError if none of the paths exist.
    """
    for path in paths:
        if os.path.isfile(path):
            print(f"Found \"faq_config.json\" at \"{path}\"!")
            with open(path, "r") as f:
                return json.load(f)
    raise FileNotFoundError(
        "Could not find \"faq_config.json\" in any of the default paths.")


def generate_question_answer_pairs(faqs):
    """
    Generates question-answer pairs from the FAQs, where each question is paired with its correct
    answer (positive sample) and other incorrect answers (negative samples).
    """
    questions, answers, scores = [], [], []

    # Precompute all answers for negative samples
    all_answers = [faq["answer"] for faq in faqs]

    for faq in faqs:
        correct_answer = faq["answer"]
        for question in faq["matched_questions"]:
            # Positive sample (correct answer)
            questions.append(question)
            answers.append(correct_answer)
            scores.append(1.0)

            # Negative samples (incorrect answers)
            for other_answer in all_answers:
                if other_answer != correct_answer:
                    questions.append(question)
                    answers.append(other_answer)
                    scores.append(0.0)

    return Dataset.from_dict({
        "sentence1": questions,
        "sentence2": answers,
        "score": scores,
    })


def split_dataset(dataset, eval_percent):
    """Splits the dataset into training and evaluation sets based on the evaluation percentage."""
    if eval_percent > 0:
        split = dataset.train_test_split(test_size=eval_percent)
        return split["train"], split["test"]
    return dataset, None


# Load FAQ configuration
faq_config = load_faq_config(faq_config_paths)

# Generate dataset and split if in eval mode
print("Generating datasets...")
train_data, eval_data = split_dataset(
    generate_question_answer_pairs(faq_config["faqs"]), eval_percent)

print(
    f"Generated datasets: \n  > Train: {train_data.num_rows} entries\n  > Eval: {0 if eval_data is None else eval_data.num_rows} entries")

# Load the model
print("Loading model to fine-tune...")
model = SentenceTransformer(base_model, cache_folder=model_cache)

# Set training arguments
args = SentenceTransformerTrainingArguments(
    output_dir=checkpoint_path,
    num_train_epochs=80,
    per_device_train_batch_size=128,
    per_device_eval_batch_size=128,
    learning_rate=0.00005 * math.sqrt(128 / 16),
    warmup_ratio=0.1,
    fp16=True,
    bf16=False,
    eval_strategy="steps" if eval_mode else "no",
    eval_steps=2000 if eval_mode else 0,
    save_strategy="steps",
    save_steps=2000,
    save_total_limit=2,
    logging_steps=500,
    run_name=model_name,
    do_eval=eval_mode,
    eval_on_start=eval_mode,
)

# Define evaluator if in eval mode
dev_evaluator = None
if eval_mode:
    dev_evaluator = EmbeddingSimilarityEvaluator(
        sentences1=eval_data["sentence1"],
        sentences2=eval_data["sentence2"],
        scores=eval_data["score"],
        main_similarity=SimilarityFunction.COSINE,
    )

# Fine-tune the model
# > CoSENTLoss - Slow to converge, doesn't overfit
# > AnglELoss - Has trouble converging
# > CosineSimilarityLoss - Overfits hard past 1 epoch
print("Fine-tuning the model...")
trainer = SentenceTransformerTrainer(
    model=model,
    args=args,
    train_dataset=train_data,
    eval_dataset=eval_data,
    loss=CoSENTLoss(model),
    evaluator=dev_evaluator,
)

trainer.train(resume_from_checkpoint=False)
model.save_pretrained(output_path)
