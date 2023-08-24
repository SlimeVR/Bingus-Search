FROM python:3-slim

WORKDIR /usr/src/app

COPY torch-cpu-requirements.txt ./
RUN pip install --no-cache-dir -r torch-cpu-requirements.txt

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["python", "./encoder-service.py"]
