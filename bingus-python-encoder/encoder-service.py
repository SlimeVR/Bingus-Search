import json
from sentence_transformers import SentenceTransformer
from fastapi import FastAPI
from pydantic import BaseModel


class EncodeRequest(BaseModel):
    sentence: str


with open("./config/encoder_config.json") as f:
    config = json.load(f)

model_file = config["model"]
print(f"Loading model \"{model_file}\"...")
model = SentenceTransformer(model_file, cache_folder="./model-cache/")
dimensions = model.get_sentence_embedding_dimension()
print(f"Model \"{model_file}\" loaded with dimension {dimensions}.")
app = FastAPI()


@app.get("/dimensions/")
async def get_dimensions():
    return {"dimensions": dimensions}


@app.post("/encode/")
async def encode_sentence(encode_request: EncodeRequest):
    return {"embedding": [val.item() for val in model.encode(encode_request.sentence)]}
