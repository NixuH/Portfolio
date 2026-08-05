import io
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
        self.buffer = None

    def __enter__(self):
        self.buffer = io.StringIO()
        if (not self.overwrite) and (self.path.exists()):
            with self.path.open("r", encoding="utf-8") as f:
                self.buffer.write(f.read())

        return self.buffer

    def __exit__(self, exc_type, exc_val, traceback):
        try:
            self.path.parent.mkdir(parents=True, exist_ok=True)
            if exc_type is None:
                with self.path.open("w", encoding=self.encoding) as f:
                    f.write(self.buffer.getvalue())
        except OSError as e:
            logger.error("Failed to write file: %s.", self.path, exc_info=e)
        finally:
            self.buffer.close()
        return False
