import os
import json
from datasets import Dataset


def load_faq_config(paths: list[str]) -> dict:
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


def generate_question_pairs(faqs: list[list[str]]) -> Dataset:
    """
    Generates question-to-question pairs from the FAQs, where each question is paired with all
    other questions in its set (positive samples) and from other sets (negative sample).
    """
    questions1, questions2, scores = [], [], []

    for faq_set in faqs:
        for question in faq_set:
            # Positive samples (same set)
            for other_question in faq_set:
                if question != other_question:
                    questions1.append(question)
                    questions2.append(other_question)
                    scores.append(1.0)

            # Negative samples (different sets)
            for other_faq_set in faqs:
                if faq_set != other_faq_set:
                    for other_question in other_faq_set:
                        questions1.append(question)
                        questions2.append(other_question)
                        scores.append(0.0)

    return Dataset.from_dict({
        "sentence1": questions1,
        "sentence2": questions2,
        "score": scores,
    })


def generate_question_answer_pairs(faqs: dict) -> Dataset:
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


def split_dataset(dataset: Dataset, eval_percent: float | int) -> tuple[Dataset, Dataset | None]:
    """Splits the dataset into training and evaluation sets based on the evaluation percentage."""
    if eval_percent > 0:
        split = dataset.train_test_split(test_size=eval_percent)
        return split["train"], split["test"]
    return dataset, None
