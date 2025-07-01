FROM python:3.14-slim

WORKDIR /usr/src/app

# curl is needed for healthcheck
RUN apt update && apt install -y curl && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*

COPY ./requirements/ ./requirements/
RUN pip install --no-cache-dir -r ./requirements/torch-cpu-runtime.txt
RUN pip install --no-cache-dir -r ./requirements/runtime.txt

COPY . .

CMD ["python", "./encoder-uvicorn.py"]
