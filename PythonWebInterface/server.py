from flask import Flask, render_template, request, jsonify
import translator
import sample_manager

app = Flask(__name__)
app.template_folder = '.'


@app.route('/translate', methods=['POST'])
def translate():
    json = request.get_json()
    source = json['source']
    expected = json['expected']

    result, errors = translator.translate(source)
    valid = translator.validate(result, expected)

    return {
        'result': result,
        'errors': errors,
        'valid': valid,
    }


@app.route('/samples/<name>', methods=['POST', 'PUT'])
def save_sample(name):
    json = request.get_json()
    source = json['source']
    expected = json['expected']

    sample_manager.save_sample(name, source, expected)

    return {
        'result': 'ok',
    }


@app.route('/samples', methods=['GET'])
def get_samples():
    return jsonify(sample_manager.get_samples())


@app.route('/samples/<name>', methods=['GET'])
def get_sample(name):
    source, expected = sample_manager.get_sample(name)

    return {
        'source': source,
        'expected': expected,
    }


@app.route('/samples/<name>', methods=['DELETE'])
def delete_sample(name):
    sample_manager.delete_sample(name)
    return {"result": "ok"}


@app.route('/')
def index():
    return render_template('page.html')


if __name__ == '__main__':
    app.run(port=8000)
