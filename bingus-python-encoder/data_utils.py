import os
from typing import TypeAlias
from pydantic import BaseModel
from datasets import Dataset
from typo import StrErrer
from random import Random

RandomSeed: TypeAlias = int | float | str | bytes | bytearray | None


def split_dataset(dataset: Dataset, eval_percent: float | int) -> tuple[Dataset, Dataset | None]:
    """Splits the dataset into training and evaluation sets based on the evaluation percentage."""
    if eval_percent > 0:
        split = dataset.train_test_split(test_size=eval_percent)
        return split["train"], split["test"]
    return dataset, None


def generate_entry_pairs(entries: list[list[str]]) -> Dataset:
    """
    Generates item-to-item pairs from the entry list, where each item is paired with all
    other item in its set (positive samples) and from other sets (negative sample).
    """
    items1, items2, scores = [], [], []

    for i, entry in enumerate(entries):
        for j, item in enumerate(entry):
            # Positive samples (same set)
            for _, other_item in enumerate(entry, start=j + 1):
                items1.append(item)
                items2.append(other_item)
                scores.append(1.0)

            # Negative samples (different sets)
            for _, other_entry in enumerate(entries, start=i + 1):
                for other_item in other_entry:
                    items1.append(item)
                    items2.append(other_item)
                    scores.append(0.0)

    return Dataset.from_dict({
        "sentence1": items1,
        "sentence2": items2,
        "score": scores,
    })


def random_typo(str_err: StrErrer, random: Random) -> StrErrer:
    """Applies a random typo to a string."""
    typo_type = random.randint(0, 8)
    if typo_type == 0:
        return str_err.char_swap()
    if typo_type == 1:
        return str_err.missing_char()
    if typo_type == 2:
        return str_err.extra_char()
    if typo_type == 3:
        return str_err.nearby_char()
    if typo_type == 4:
        return str_err.similar_char()
    if typo_type == 5:
        return str_err.skipped_space()
    if typo_type == 6:
        return str_err.random_space()
    if typo_type == 7:
        return str_err.repeated_char()
    return str_err.unichar()


class FaqEntry(BaseModel):
    title: str | None
    answer: str
    matched_questions: list[str]


class FaqConfig(BaseModel):
    faqs: list[FaqEntry]

    @staticmethod
    def load_from_file(paths: list[str] | str):
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

    def save_to_file(self, path: str):
        """
        Saves a faq_config.json file to the specified path.
        """
        with open(path, "w") as f:
            f.write(self.model_dump_json())

    def generate_typos(
            self,
            entry_variants: int,
            min_typos: int,
            max_typos: int,
            scale_max_per_word: bool = True,
            scale_min_per_word: bool = False,
            seed: RandomSeed = None
    ) -> tuple[int, int]:
        """
        Generates typos for each question of each entry and returns the number of entries added and the
        number of typos generated.
        """
        if entry_variants < 1:
            raise ValueError(
                "entry_variants must be greater than or equal to 1")
        if min_typos < 0:
            raise ValueError("min_typos must be greater than or equal to 0")
        if max_typos < 1:
            raise ValueError("max_typos must be greater than or equal to 1")
        if min_typos > max_typos:
            raise ValueError(
                "min_typos must be less than or equal to max_typos")

        seeded_random = Random(seed)
        typo_entries = 0
        typo_count = 0
        for faq in self.faqs:
            new_faqs: list[str] = []

            for question in faq.matched_questions:
                q_min_typos = min_typos
                q_max_typos = max_typos
                if scale_max_per_word:
                    num_words = len(question.split())
                    q_max_typos *= num_words
                    if scale_min_per_word:
                        q_min_typos *= num_words

                for _ in range(entry_variants):
                    num_typos = seeded_random.randint(q_min_typos, q_max_typos)
                    typo_faq = StrErrer(question, seed=seeded_random.random())
                    for _ in range(num_typos):
                        typo_faq = random_typo(typo_faq, seeded_random)
                    new_faqs.append(typo_faq.result)
                    typo_count += num_typos

            faq.matched_questions.extend(new_faqs)
            typo_entries += len(new_faqs)

        return typo_entries, typo_count

    def generate_question_pairs(self) -> Dataset:
        """
        Generates question-to-question pairs from the FAQs, where each question is paired with all
        other questions in its set (positive samples) and from other sets (negative sample).
        """
        return generate_entry_pairs([faq.matched_questions for faq in self.faqs])

    def generate_question_answer_pairs(self, include_title: bool = True) -> Dataset:
        """
        Generates question-answer pairs from the FAQs, where each question is paired with its correct
        answer (positive sample) and other incorrect answers (negative samples).
        """
        questions, answers, scores = [], [], []

        # Precompute all answers for negative samples
        all_answers = [faq.answer for faq in self.faqs]

        for faq in self.faqs:
            for question in faq.matched_questions:
                # Positive sample (correct answer)
                questions.append(question)
                answers.append(faq.answer)
                scores.append(1.0)

                # Negative samples (incorrect answers)
                for other_answer in all_answers:
                    if other_answer != faq.answer:
                        questions.append(question)
                        answers.append(other_answer)
                        scores.append(0.0)

            if include_title and faq.title != None:
                # Positive sample (correct answer)
                questions.append(faq.title)
                answers.append(faq.answer)
                scores.append(1.0)

                # Negative samples (incorrect answers)
                for other_answer in all_answers:
                    if other_answer != faq.answer:
                        questions.append(faq.title)
                        answers.append(other_answer)
                        scores.append(0.0)

        return Dataset.from_dict({
            "sentence1": questions,
            "sentence2": answers,
            "score": scores,
        })

    def generate_everything_pairs(self) -> Dataset:
        """
        Generates pairs of titles, answers, and questions from the FAQs, where each set is paired with its correct
        answer (positive sample) and other incorrect answers (negative samples).
        """
        return generate_entry_pairs([[faq.title, faq.answer, *faq.matched_questions] for faq in self.faqs])
