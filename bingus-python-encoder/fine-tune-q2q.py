from data_utils import load_faq_config, generate_question_pairs, split_dataset
from sentence_transformers import SentenceTransformer, SentenceTransformerTrainer, SentenceTransformerTrainingArguments
from sentence_transformers.losses import CoSENTLoss
from sentence_transformers.evaluation import EmbeddingSimilarityEvaluator, SimilarityFunction
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
model_ver = 13
model_name = f"Bingus-v{model_ver}{'_Eval' if eval_mode else ''}_{base_model}"
model_dir = f"./local-models/{model_name}/"
output_path = f"{model_dir}{model_name}/"
checkpoint_path = f"{model_dir}checkpoints/"

# Load FAQ configuration
faq_config = load_faq_config(faq_config_paths)

# Generate dataset and split if in eval mode
print("Generating datasets...")
train_data, eval_data = split_dataset(generate_question_pairs(
    [faq["matched_questions"] for faq in faq_config["faqs"]]), eval_percent)

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
    logging_first_step=True,
    logging_strategy="epoch",
    logging_steps=1,
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
