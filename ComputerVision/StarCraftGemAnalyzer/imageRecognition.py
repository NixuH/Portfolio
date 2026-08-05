# Commented-out code is not needed for this sample,
# but it shows how the problem was originally solved.

import cv2 as cv
import numpy as np
import winsound
import pyautogui
from enum import Enum

# import os
# import mss
# import keyboard


class MatchingMethod(Enum):
    """Available OpenCV template matching methods."""

    CCOEFF = cv.TM_CCOEFF_NORMED
    SQDIFF = cv.TM_SQDIFF_NORMED


# def get_screen_shot() -> str:
#     """Captures the selected screen area and saves it as an image.

#     Returns:
#         str: Path to the saved screenshot.
#     """
#     with mss.mss() as sct:
#         output = "sct-{top}x{left}_{width}x{height}.png".format(**monitor)

#         sct_img = sct.grab(monitor)

#         mss.tools.to_png(sct_img.rgb, sct_img.size, output=output)

#         return output


def find_in_img(
    img: np.ndarray,
    base_img: np.ndarray,
    threshold: float,
    line_color: tuple[int, int, int],
    method: MatchingMethod,
    symbol: str,
    board: list[list[str]],
    debug_img: np.ndarray,
) -> None:
    """Detects objects in an image using template matching.

    Searches for occurrences of a template image inside the current
    screenshot using OpenCV template matching. Detected objects are
    highlighted on the debug image and their positions are stored
    in the game board.

    Args:
        img (np.ndarray): Template image used for detection.
        base_img (np.ndarray): Screenshot searched for the template.
        threshold (float): Matching threshold used to filter results.
        line_color (tuple[int, int, int]): BGR color used for drawing detection rectangles.
        method (MatchingMethod): Template matching algorithm used by OpenCV.
        symbol (str): Symbol representing the detected object in the board.
        board (list[list[str]]): Board representation modified in-place with detected object positions.
        debug_img (np.ndarray): Image on which detection results are drawn.

    """

    result = cv.matchTemplate(base_img, img, method.value)
    if method == MatchingMethod.CCOEFF:
        locations = np.where(result >= threshold)
    else:
        locations = np.where(result <= threshold)

    locations = list(zip(*locations[::-1]))

    if locations:

        rectangles = []
        for loc in locations:
            rect = [
                int(loc[0]) + 5,
                int(loc[1]) + 5,
                img.shape[1] - 10,
                img.shape[0] - 10,
            ]
            rectangles.append(rect)
            rectangles.append(rect)

        rectangles, weights = cv.groupRectangles(rectangles, 1, 0.5)

        for x, y, w, h in rectangles:
            mid_w, mid_h = (x + (w / 2)) - margin_x, (y + (h / 2)) - margin_y
            b_y, b_x = int(mid_h // tile_size), int(mid_w // tile_size)

            cv.rectangle(debug_img, (x, y), (x + w, y + h), line_color, 6, line_type)

            board[b_y][b_x] = symbol


gas = cv.imread("./jewels/1.jpg", cv.IMREAD_COLOR)
zerg = cv.imread("./jewels/2.jpg", cv.IMREAD_COLOR)
protos = cv.imread("./jewels/3.jpg", cv.IMREAD_COLOR)
minerals = cv.imread("./jewels/4.jpg", cv.IMREAD_COLOR)
dominium = cv.imread("./jewels/5.jpg", cv.IMREAD_COLOR)
rackeeters = cv.imread("./jewels/6.jpg", cv.IMREAD_COLOR)


# Config

margin_x, margin_y, screen_shot_w, screen_shot_h = 0, 0, 864, 864  # board params

screen_x, screen_y = 1957, 155  # board pixels start

board_w = 8  # number of tile_sizes
board_h = 8  # number of tile_sizes
tile_size = 108  # in px

# Config


# monitor = {
#     "top": screen_y,
#     "left": screen_x,
#     "width": screen_shot_w,
#     "height": screen_shot_h,
# }

line_type = cv.LINE_4

gas_symbol = "*"
zerg_symbol = "&"
protos_symbol = "^"
minerals_symbol = "$"
dominium_symbol = "!"
rackeeters_symbol = "@"

gems = [
    (gas, (0, 255, 0), gas_symbol),
    (zerg, (255, 0, 255), zerg_symbol),
    (protos, (0, 255, 255), protos_symbol),
    (minerals, (255, 0, 0), minerals_symbol),
    (dominium, (0, 0, 255), dominium_symbol),
    (rackeeters, (150, 150, 150), rackeeters_symbol),
]


def new_board(height: int, width: int) -> list[list[str]]:
    """Creates an empty game board.

    Args:
        height (int): Number of rows in the board.
        width (int): Number of columns in the board.

    Returns:
        list[list[str]]: Two-dimensional list representing an empty board.
    """
    return [["" for _ in range(width)] for _ in range(height)]


def scan_img(base_img: np.ndarray) -> list[list[str]]:
    """Scans the current screenshot for all available gem types.

    Runs template matching for each gem template, updates the internal
    board representation and saves an image containing detection results.

    Args:
        base_img (np.ndarray): Screenshot image to analyze.

    Returns:
        list[list[str]]: Two-dimensional board representation containing
        detected gem symbols.
    """
    board = new_board(board_h, board_w)
    result_img = base_img.copy()

    for image, color, symbol in gems:
        find_in_img(
            image,
            base_img,
            0.04,
            color,
            MatchingMethod.SQDIFF,
            symbol,
            board,
            result_img,
        )

    cv.imwrite("result_img.png", result_img)

    return board


# def board_to_mouse_position(res):
#     """Converts board coordinates into screen coordinates.

#     Args:
#         res (list[list[int]]): Two board positions.

#     Returns:
#         list[list[int]]: Mouse positions in screen coordinates.
#     """
#     from_y, from_x = res[0]

#     to_y, to_x = res[1]

#     from_y = (from_y * (tile_size + 1)) + screen_y + tile_size // 2
#     from_x = (from_x * (tile_size + 1)) + screen_x + tile_size // 2

#     to_y = (to_y * (tile_size + 1)) + screen_y + tile_size // 2
#     to_x = (to_x * (tile_size + 1)) + screen_x + tile_size // 2

#     return [[from_x, from_y], [to_x, to_y]]


def do_mouse(from_pos: tuple[int, int], to_pos: tuple[int, int]) -> None:
    """Performs a mouse action between two positions.

    Moves the cursor to the first position, clicks, then moves to the
    second position and clicks again.

    Args:
        from_pos (tuple[int, int]): Starting mouse position.
        to_pos (tuple[int, int]): Target mouse position.
    """
    pyautogui.moveTo(from_pos)
    pyautogui.leftClick()
    pyautogui.moveTo(to_pos)
    pyautogui.leftClick()


# Choose one of 10 samples
# screenShot = cv.imread("Sample1.jpg", cv.IMREAD_COLOR)
screenShot = cv.imread("Sample2.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample3.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample4.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample5.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample6.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample7.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample8.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample9.jpg", cv.IMREAD_COLOR)
# screenShot = cv.imread("Sample10.jpg", cv.IMREAD_COLOR)

board = scan_img(screenShot)

for row in board:
    print(row)

winsound.Beep(440, 75)
winsound.Beep(200, 100)

# That was main loop in program but no needed for the sample
# while True:
#     keyboard.wait("j")
#     print("start")
#     winsound.Beep(440, 75)
#     winsound.Beep(700, 100)
#     time.sleep(0.5)
#     try:
#         while True:
#             board = new_board(board_h, board_w)
#             screenShot = get_screen_shot()
#             screenShot = cv.imread(screenShot, cv.IMREAD_COLOR)
#             scan_img()

#             if keyboard.is_pressed("j"):
#                 print("stop")
#                 winsound.Beep(440, 75)
#                 winsound.Beep(200, 100)
#                 time.sleep(0.5)
#                 print(board)
#                 break

#             time.sleep(0.01)

#     except KeyboardInterrupt:
#         print("Script stopped.")
