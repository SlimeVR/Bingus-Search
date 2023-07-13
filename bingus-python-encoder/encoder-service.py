from sentence_transformers import SentenceTransformer
from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn

class EncodeRequest(BaseModel):
    sentence: str

model = SentenceTransformer("all-mpnet-base-v2", cache_folder = "./model-cache/")
dimensions = len(model.encode(""))
app = FastAPI()

@app.get("/dimensions/")
async def get_dimensions():
    return {"dimensions": dimensions}

@app.post("/encode/")
async def encode_sentence(encode_request: EncodeRequest):
    return {"embedding": [val.item() for val in model.encode(encode_request.sentence)]}

if __name__ == "__main__":
    uvicorn.run("encoder-service:app", host="0.0.0.0", port=5000, log_level="info")
