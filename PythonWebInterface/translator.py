import os
import subprocess


def translate(source: str) -> [str, str]:
    temp_file_source = "./temp/source.rs"
    with open(temp_file_source, "w") as f:
        f.write(source)

    process = subprocess.run([os.getenv("TRANSLATOR_BINARY"), temp_file_source], capture_output=True)

    result = process.stdout.decode("utf-8")
    errors = process.stderr.decode("utf-8")
    return result, errors


def validate(translated: str, expected: str) -> bool:
    i, j = 0, 0
    tokens = {}

    translated += "!EOF!"
    expected += "!EOF!"

    while i < len(translated) and j < len(expected):
        if translated[i] == expected[j]:
            i += 1
            j += 1
        elif translated[i].isspace():
            i += 1
        elif expected[j].isspace():
            j += 1
        elif expected[j] == "_":
            token = ""
            j += 1
            while expected[j] != "_":
                token += expected[j]
                j += 1
            j += 1
            result_token = ""
            while translated[i].isalnum():
                result_token += translated[i]
                i += 1
            if token != "" and (token in tokens and tokens[token] != result_token or result_token in tokens.values()):
                return False
            tokens[token] = result_token
        else:
            return False

    return True
