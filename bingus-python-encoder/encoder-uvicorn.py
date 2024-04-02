if __name__ == "__main__":
    import json
    import uvicorn

    with open("./config/encoder_config.json") as f:
        config = json.load(f)

    uvicorn.run("encoder-service:app", host=config["host"],
                port=config["port"], log_level="info")
