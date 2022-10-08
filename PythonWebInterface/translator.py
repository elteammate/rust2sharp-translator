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
    return translated == expected
