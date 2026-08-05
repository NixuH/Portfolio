import logging
from abc import ABC, abstractmethod
from functools import total_ordering

logger = logging.getLogger("mediaLogger")

if not logger.handlers:
    logger.setLevel(logging.INFO)

    handler = logging.FileHandler("app.log")
    formatter = logging.Formatter("%(asctime)s [%(levelname)s] %(name)s: %(message)s")

    handler.setFormatter(formatter)
    logger.addHandler(handler)


@total_ordering
class Media(ABC):  # inherit ABC for abstract method
    def __init__(self, title: str, author: str, year: int, description: str, content):
        self.title = title
        self.author = author
        self.year = year
        self.description = description
        self.content = content

    def __eq__(self, other):
        if isinstance(other, Media):
            return self.year == other.year
        if isinstance(other, int) or isinstance(other, float):
            return self.year == other
        return NotImplemented

    def __lt__(self, other):
        if isinstance(other, Media):
            return self.year < other.year
        if isinstance(other, int) or isinstance(other, float):
            return self.year < other
        return NotImplemented

    # access by obj.author (getter). (No needed just example)
    # @property
    # def author(self):
    #     return self._author

    # repr is for debug
    def __repr__(self):
        return f"Title: {self.title}, Author: {self.author}, Year: {self.year}"

    # str is for print
    def __str__(self):
        return f"Title: {self.title}, Author: {self.author}, Year: {self.year}"

    def getDescription(self):
        return self.description

    def getAuthor(self):
        return self.author

    def getYear(self):
        return self.year

    def getInfo(self):
        return {
            "author": self.author,
            "year": self.year,
            "description": self.description,
            "content": self.content,
            "type": self.__class__.__name__,
        }

    @abstractmethod
    def getContent(self):
        pass

    @abstractmethod
    def __add__(self, other):
        pass

class CD(Media):
    def __init__(
        self, title: str, author: str, year: int, description: str, content: int
    ):
        super().__init__(title, author, year, description, content)
        if not (isinstance(self.content, int)):
            logger.warning("CD content is not integer type")

    def play(self):
        return self.content

    def getContent(self):
        return self.play()
    
    def __add__(self, other):
        if isinstance(other, CD):
            self.content += other.content
        if isinstance(other, list):
            for i in other:
                self.content+= " " + i
        if isinstance(other, int):
            self.content+=other
        raise ValueError("Bad other type")


class Game(Media):
    def __init__(
        self, title: str, author: str, year: int, description: str, content: str
    ):
        super().__init__(title, author, year, description, content)
        if not (isinstance(self.content, str)):
            logger.warning("Game content is not string type")

    def run(self):
        return self.content

    def getContent(self):
        return self.run()
    
    def __add__(self, other):
        if isinstance(other, CD):
            self.content += other.content
        if isinstance(other, list):
            for i in other:
                self.content+= " " + i
        if isinstance(other, str):
            self.content+=other
        raise ValueError("Bad other type")
