FROM python:3.11-slim

WORKDIR /app

RUN pip install kubernetes requests

COPY ca.crt .
COPY collect_info.py .

CMD ["python", "collect_info.py"]