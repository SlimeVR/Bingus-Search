FROM python:3.11-slim

WORKDIR /usr/src/app

# curl is needed for healthcheck
RUN apt update && apt install -y curl

COPY ./requirements/ ./requirements/
RUN pip install --no-cache-dir -r ./requirements/torch-cpu-runtime.txt
RUN pip install --no-cache-dir -r ./requirements/runtime.txt

COPY . .

CMD ["python", "./encoder-service.py"]
