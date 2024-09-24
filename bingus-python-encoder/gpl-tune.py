import gpl

dataset = "slimevr"
gpl.train(
    path_to_generated_data=f"generated/{dataset}",
    # The path with corpus.jsonl, otherwise use evaluation_data
    base_ckpt="all-MiniLM-L6-v2",
    # base_ckpt='GPL/msmarco-distilbert-margin-mse',
    # The starting checkpoint of the experiments in the paper
    batch_size_gpl=32,
    gpl_steps=60000,
    output_dir=f"output/{dataset}",
    evaluation_data=f"./{dataset}",
    evaluation_output=f"evaluation/{dataset}",
    # This prefix will appear as part of the (folder/file) names for query-generation results: For example, we will have "qgen-qrels/" and "qgen-queries.jsonl" by default.
    do_evaluation=False
)
