import json
import numpy as np
import os
import pickle
from keras.models import load_model

from azureml.core.model import Model

def init():
    global model
    # retrieve the path to the model file using the model name
    model_path = Model.get_model_path('model.h5')
    model = load_model(model_path)

def run(raw_data):
    data = np.asarray(json.loads(raw_data)['data'])
    return model.predict(data)
