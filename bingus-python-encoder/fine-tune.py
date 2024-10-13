from data_utils import FaqConfig, split_dataset
from sentence_transformers import SentenceTransformer, SentenceTransformerTrainer, SentenceTransformerTrainingArguments
from sentence_transformers.losses import CoSENTLoss
from sentence_transformers.evaluation import EmbeddingSimilarityEvaluator, SimilarityFunction
import math
import os

# Load FAQ configuration
faq_config = FaqConfig.load_from_file([
    "./faq_config.json",
    "../BingusApi/config/faq_config.json",
    "./BingusApi/config/faq_config.json"
])
print(
    f"Loaded FAQ config:\n  > {len(faq_config.faqs)} FAQs\n  > {faq_config.question_count()} questions")

# FAQ modifiers
generate_faq_typos = True
save_modified_faq = True

# Data pairing mode
# 0: Question to question (q2q)
# 1: Question to answer (q2a)
# 2: Everything to everything (e2e)
pairing_modes = ["q2q", "q2a", "e2e"]
pairing_mode = 1
pairing_mode_name = pairing_modes[pairing_mode]

# Evaluation settings
eval_mode = False
eval_percent = 0.2 if eval_mode else 0
eval_name = "_Eval" if eval_mode else ""

# Input model settings
model_cache = "./model-cache/"
base_model = "all-MiniLM-L6-v2"

# Output model settings
model_ver = 4
model_name = f"Bingus-{pairing_mode_name}-v{model_ver}{eval_name}_{base_model}"
model_dir = f"./local-models/{model_name}/"
os.makedirs(model_dir, exist_ok=True)

# Modify FAQ config
print("Filtering short questions...")
faq_config.filter_short_questions(4)
print(
    f"Filtered FAQ config:\n  > {len(faq_config.faqs)} FAQs\n  > {faq_config.question_count()} questions")

if generate_faq_typos:
    print("Generating typos...")
    typo_entry_count, typo_count = faq_config.generate_typos(
        entry_variants=3,
        min_typos=1,
        max_typos=2,
        scale_max_per_word=True,
        scale_min_per_word=True,
        per_word_multiplier=0.2,
        seed=42
    )
    print(
        f"Generated {typo_entry_count} new questions with {typo_count} typos.")

if save_modified_faq:
    faq_output = f"{model_dir}faq_config.json"
    faq_config.save_to_file(faq_output)
    print(f"Saved modified FAQ to \"{faq_output}\".")

# Generate dataset and split if in eval mode
print("Generating datasets...")
if (pairing_mode == 0):
    dataset = faq_config.generate_question_pairs()
elif (pairing_mode == 1):
    dataset = faq_config.generate_question_answer_pairs(False)
elif (pairing_mode == 2):
    dataset = faq_config.generate_everything_pairs()
else:
    raise ValueError(f"Invalid pairing mode: {pairing_mode}")
train_data, eval_data = split_dataset(dataset, eval_percent)

print(
    f"Generated datasets:\n  > Train: {train_data.num_rows} entries\n  > Eval: {0 if eval_data is None else eval_data.num_rows} entries")

# Load the model
print("Loading model to fine-tune...")
model = SentenceTransformer(base_model, cache_folder=model_cache)

# Set training arguments
args = SentenceTransformerTrainingArguments(
    output_dir=f"{model_dir}checkpoints/",
    num_train_epochs=2,
    per_device_train_batch_size=128,
    per_device_eval_batch_size=128,
    learning_rate=0.00005 * math.sqrt(128 / 16),
    warmup_ratio=0.1,
    fp16=True,
    bf16=False,
    eval_strategy="epoch" if eval_mode else "no",
    eval_steps=1 if eval_mode else 0,
    save_strategy="epoch",
    save_steps=1,
    save_total_limit=None,
    logging_first_step=False,
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
model.save_pretrained(f"{model_dir}{model_name}/")
