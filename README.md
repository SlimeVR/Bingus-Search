# Bingus

## Compiling ONNX Runtime Extensions

- Link: https://github.com/microsoft/onnxruntime-extensions
- Reference: https://github.com/microsoft/onnxruntime-extensions/blob/main/docs/development.md

To compile ONNX Runtime Extensions, run the following commands:

```bash
git clone --recurse-submodules https://github.com/microsoft/onnxruntime-extensions.git
cd onnxruntime-extensions
```

### For Windows

```cmd
rem Run the provided build script for Windows
build.bat
```

### For Linux

```bash
# Run the provided build script for Linux
bash ./build.sh
```

The output file will be quite large (100+ MB), so to reduce the size, you can strip all debug information with this command:

```bash
strip --strip-all libortextensions.so
```

## Converting TensorFlow model to ONNX model

- Link: https://github.com/onnx/tensorflow-onnx

To convert the TensorFlow model to an ONNX model, you will need to have the ONNX Runtime Extensions, then run the following commands:

```bash
# Install required packages
pip install -U onnx tensorflow tensorflow_text tf2onnx

# Convert the model
python -m tf2onnx.convert --saved-model ./models/tensorflow/use_l_v5/ --output ./models/onnx/use_l_v5.onnx --load_op_libraries libortextensions.so --opset 17 --extra_opset ai.onnx.contrib:1
```
