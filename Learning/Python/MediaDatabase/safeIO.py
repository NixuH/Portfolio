import csv
import pathlib
import logging


logger = logging.getLogger("SafeIOLogger")

if not logger.handlers:
    logger.setLevel(logging.INFO)
    handler = logging.FileHandler("app.log")
    formatter = logging.Formatter("%(asctime)s [%(levelname)s] %(name)s: %(message)s")
    handler.setFormatter(formatter)
    logger.addHandler(handler)


class SafeIO:
    def __init__(self, path: str, encoding="utf-8", overwrite=False):
        self.path = pathlib.Path(path)
        self.encoding = encoding
        self.overwrite = overwrite
        self.buffer = []

    def append(self, other):
        if isinstance(other, list):
            for i in other:
                self.append(i)
            return
        if isinstance(other, dict):
            if len(self.buffer) == 0:
                self.buffer.append(other)
                return

            tmp = dict()
            for i in list(self.buffer[0].keys()):
                tmp[i] = ""
            for i in list(other.keys()):
                if i not in tmp.keys():
                    self.extendDict(i)
                tmp[i] = other[i]

            self.buffer.append(tmp)

    def find(self, key: str, val):
        for i, data in enumerate(self.buffer):
            if data[key] == val:
                return i
        return None

    def remove(self, key: str, val):
        i = self.find(key, val)
        if i != None:
            del self.buffer[i]
            return True

        return False

    def extendDict(self, key: str):
        for i in range(len(self.buffer)):
            self.buffer[i][key] = ""

    def __enter__(self):
        if (not self.overwrite) and (self.path.exists()):
            with self.path.open("r", encoding=self.encoding) as f:
                reader = csv.DictReader(f)
                self.buffer = [row for row in reader]

        return self

    def __exit__(self, exc_type, exc_val, traceback):
        self.path.parent.mkdir(parents=True, exist_ok=True)

        if len(self.buffer) == 0:
            with self.path.open("w", encoding=self.encoding):
                pass
            return False

        try:
            if exc_type is None:
                with self.path.open("w", encoding=self.encoding, newline="") as f:
                    fieldnames = list(self.buffer[0].keys())
                    writer = csv.DictWriter(f, fieldnames=fieldnames)
                    writer.writeheader()
                    writer.writerows(self.buffer)

        except OSError as e:
            logger.error("Failed to write file: %s.", self.path, exc_info=e)

        return False
