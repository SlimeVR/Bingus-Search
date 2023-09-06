from sentence_transformers import SentenceTransformer, InputExample, losses, evaluation
from torch.utils.data import DataLoader

import os
import json
import math
import shutil

# Define the default paths to check
faq_config_paths = ["./faq_config.json",
                    "../BingusApi/faq_config.json", "./BingusApi/faq_config.json"]
faq_config_path = None
faq_config = None

# Search the paths for the config
for cur_path in faq_config_paths:
    if os.path.isfile(cur_path):
        print(f"Found \"faq_config.json\" at \"{cur_path}\"!")
        faq_config_path = cur_path
        break
    else:
        print(f"Could not find \"faq_config.json\" at \"{cur_path}\".")

if faq_config_path is None:
    raise FileNotFoundError(
        "Could not find \"faq_config.json\" in any of the default paths.")

# Load the config
print("Reading FAQ config...")
with open(faq_config_path, "r") as f:
    faq_config = json.load(f)
faqs = [faq["matched_questions"] for faq in faq_config["faqs"]]
faq_count = sum(len(faq) for faq in faqs)

# Set up the dataset by pairing each question with another one
print("Generating datasets...")
# Training dataset store
training_set = []

# Eval dataset stores
sentences1 = []
sentences2 = []
scores = []

# Set up eval collection
include_eval_in_other_pairs = False
include_eval_in_training = False
eval_percent = 0.1
# Calculated eval params
eval_interval = math.ceil(1 / eval_percent)
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

                is_eval = include_eval_in_other_pairs and eval_count % eval_interval == 0
                eval_count += 1

                # If it's part of the same set, it's similar questions, otherwise it's not similar
                score = 1.0 if faq_set == other_faq_set else 0.0

                # Do not include any eval data in training data if it's exclusive
                if not is_exclusive_eval:
                    # Add to the training set if not eval
                    if not is_eval or include_eval_in_training:
                        training_set.append(InputExample(
                            texts=[faq, other_faq], label=score))

                # Add to eval if it's within the interval
                if is_eval or is_exclusive_eval:
                    sentences1.append(faq)
                    sentences2.append(other_faq)
                    scores.append(score)
print(
    f"Generated datasets: \n  > Training: {len(training_set)} entries\n  > Evaluation: {len(sentences1)} entries")

# Load the model to fine-tune
print("Loading model to fine-tune...")
model_cache = "./model-cache/"
model_name = "bingus-fine-tuned"
model_dir = f"./local-models/{model_name}/"
output_path = f"{model_dir}output/"
checkpoint_path = f"{model_dir}checkpoints/"
model = SentenceTransformer("all-MiniLM-L6-v2", cache_folder=model_cache)

# Define the training dataset, the data loader and the training loss
training_dataloader = DataLoader(training_set, shuffle=True, batch_size=16)
training_loss = losses.CosineSimilarityLoss(model)
evaluator = evaluation.EmbeddingSimilarityEvaluator(
    sentences1=sentences1, sentences2=sentences2, scores=scores)

# Remove any previously trained outputs
if os.path.isdir(model_dir):
    shutil.rmtree(model_dir)

# Tune the model
print("Fine-tuning the model...")
model.fit(train_objectives=[(training_dataloader, training_loss)], evaluator=evaluator, epochs=1, warmup_steps=100,
          evaluation_steps=500, output_path=output_path, checkpoint_save_steps=500, checkpoint_path=checkpoint_path)
