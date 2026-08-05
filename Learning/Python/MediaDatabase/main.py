# przygotowanie do kolokwium

import media
import safeIO
import sys
import logging

logger = logging.getLogger("AppLogger")

if not logger.handlers:
    logger.setLevel(logging.INFO)

    handler = logging.FileHandler("app.log")
    formatter = logging.Formatter("%(asctime)s [%(levelname)s] %(name)s: %(message)s")

    handler.setFormatter(formatter)
    logger.addHandler(handler)

type_map = {"Game": media.Game, "CD": media.CD}


def main():
    with safeIO.SafeIO("database.csv") as db:
        while True:
            try:
                com = int(
                    input(
                        "1. Add Media \n2. Remove media \n3. Show media \n4. Read descryption \n5. Open \n6. Exit\n"
                    )
                )
                match com:
                    case 1:
                        com1 = int(input("1. CD \n2. Game \n"))

                        val = [
                            str(input("Title (str):\n")),
                            str(input("Author (str):\n")),
                            int(input("Year (int):\n")),
                            str(input("Description (str):\n")),
                        ]

                        data = ""
                        if com1 == 1:
                            data = int(input("content (int): \n"))
                        else:
                            data = str(input("content (str): \n"))
                        val.append(data)

                        match com1:
                            case 1:
                                cls = type_map["CD"]
                            case 2:
                                cls = type_map["Game"]

                        obj = cls(val[0], val[1], val[2], val[3], val[4])

                        db.append(obj.getInfo())
                    case 2:
                        key, val = input("key:\n").lower(), input("value:\n")
                        result = db.remove(key, val)
                        if result:
                            print("removed\n")
                        else:
                            print("not found\n")
                    case 3:
                        for i in db.buffer:
                            print(i)
                    case 4:
                        key, val = input("key:\n").lower(), input("value:\n")
                        index = db.find(key, val)
                        if index is not None:
                            print(db.buffer[index]["description"])
                        else:
                            print("not found")
                    case 5:
                        key, val = input("key:\n"), input("value:\n")
                        index = db.find(key, val)
                        if index is not None:
                            data = db.buffer[index]
                            cls = type_map[data["type"]]
                            obj = cls(
                                data["title"],
                                data["author"],
                                data["year"],
                                data["description"],
                                data["content"],
                            )
                    case 6:
                        sys.exit()

            except ValueError as e:
                logger.error(e)
                print("Bad type in input. Returning to menu")
            except KeyboardInterrupt:
                logger.warning("App interrupted")
                print("Interrupted")
                sys.exit()
            except SystemExit:
                logger.info("App exit")
                print("Exit")
                sys.exit()
            except Exception as e:
                logger.error(e)
                print("Unexpected error" + e)
                print("Exit")
                sys.exit()


main()
