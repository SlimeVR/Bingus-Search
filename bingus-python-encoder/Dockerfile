FROM python:3-slim

WORKDIR /usr/src/app

COPY ./requirements/ ./requirements/
RUN pip install --no-cache-dir -r ./requirements/torch-cpu-runtime.txt
RUN pip install --no-cache-dir -r ./requirements/runtime.txt

COPY . .

CMD ["python", "./encoder-service.py"]
