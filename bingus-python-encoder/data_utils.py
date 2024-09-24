import os
from pydantic import BaseModel
from datasets import Dataset


class FaqEntry(BaseModel):
    title: str
    answer: str
    matched_questions: list[str]


class FaqConfig(BaseModel):
    faqs: list[FaqEntry]


def load_faq_config(paths: list[str]) -> FaqConfig:
    """
    Searches through a list of paths to find and load the first existing faq_config.json file.
    Raises a FileNotFoundError if none of the paths exist.
    """
    for path in paths:
        if os.path.isfile(path):
            print(f"Found \"faq_config.json\" at \"{path}\"!")
            with open(path, "r") as f:
                return FaqConfig.model_validate_json(f.read())
    raise FileNotFoundError(
        "Could not find \"faq_config.json\" in any of the default paths.")


def generate_entry_pairs(entries: list[list[str]]) -> Dataset:
    """
    Generates item-to-item pairs from the entry list, where each item is paired with all
    other item in its set (positive samples) and from other sets (negative sample).
    """
    items1, items2, scores = [], [], []

    for entry in entries:
        for item in entry:
            # Positive samples (same set)
            for other_item in entry:
                if item != other_item:
                    items1.append(item)
                    items2.append(other_item)
                    scores.append(1.0)

            # Negative samples (different sets)
            for other_entry in entries:
                if entry != other_entry:
                    for other_item in other_entry:
                        items1.append(item)
                        items2.append(other_item)
                        scores.append(0.0)

    return Dataset.from_dict({
        "sentence1": items1,
        "sentence2": items2,
        "score": scores,
    })


def generate_question_pairs(faqs: list[FaqEntry]) -> Dataset:
    """
    Generates question-to-question pairs from the FAQs, where each question is paired with all
    other questions in its set (positive samples) and from other sets (negative sample).
    """
    return generate_entry_pairs([faq.matched_questions for faq in faqs])


def generate_question_answer_pairs(faqs: list[FaqEntry]) -> Dataset:
    """
    Generates question-answer pairs from the FAQs, where each question is paired with its correct
    answer (positive sample) and other incorrect answers (negative samples).
    """
    questions, answers, scores = [], [], []

    # Precompute all answers for negative samples
    all_answers = [faq.answer for faq in faqs]

    for faq in faqs:
        correct_answer = faq.answer
        for question in faq.matched_questions:
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


def generate_everything_pairs(faqs: list[FaqEntry]) -> Dataset:
    """
    Generates pairs of titles, answers, and questions from the FAQs, where each set is paired with its correct
    answer (positive sample) and other incorrect answers (negative samples).
    """
    return generate_entry_pairs([[[faq.title, faq.answer, *faq.matched_questions] for faq in faqs]])


def split_dataset(dataset: Dataset, eval_percent: float | int) -> tuple[Dataset, Dataset | None]:
    """Splits the dataset into training and evaluation sets based on the evaluation percentage."""
    if eval_percent > 0:
        split = dataset.train_test_split(test_size=eval_percent)
        return split["train"], split["test"]
    return dataset, None
