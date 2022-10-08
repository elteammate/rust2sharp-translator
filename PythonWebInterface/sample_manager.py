import os


def save_sample(name: str, source: str, expected: str):
    with open(f'samples/{name}.rs', 'w') as f:
        f.write(f"{source}")

    with open(f'samples/{name}.expected.cs', 'w') as f:
        f.write(expected)


def get_samples() -> list[str]:
    samples = []
    for file in os.listdir('samples'):
        if file.endswith('.rs'):
            samples.append(file[:-3])
    return samples


def get_sample(name: str) -> [str, str]:
    with open(f'samples/{name}.rs', 'r') as f:
        source = f.read()

    with open(f'samples/{name}.expected.cs', 'r') as f:
        expected = f.read()

    return source, expected


def delete_sample(name: str):
    os.remove(f'samples/{name}.rs')
    os.remove(f'samples/{name}.expected.cs')
