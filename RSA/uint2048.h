#pragma once

#include <algorithm>
#include <compare>
#include <cstddef>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

using uint128 = unsigned __int128;

class uint2048
{
private:
    uint64_t data[32]{};

    static uint8_t hexValue(char c)
    {
        if (c >= '0' && c <= '9')
            return c - '0';

        if (c >= 'A' && c <= 'F')
            return c - 'A' + 10;

        if (c >= 'a' && c <= 'f')
            return c - 'a' + 10;

        throw std::invalid_argument(
            "Invalid hex character"
        );
    }

    static unsigned char addCarry(
        unsigned char carry,
        uint64_t a,
        uint64_t b,
        uint64_t& result)
    {
        uint128 sum =
            static_cast<uint128>(a) +
            static_cast<uint128>(b) +
            carry;

        result =
            static_cast<uint64_t>(sum);

        return static_cast<unsigned char>(
            sum >> 64
        );
    }

    static unsigned char subBorrow(
        unsigned char borrow,
        uint64_t a,
        uint64_t b,
        uint64_t& result)
    {
        uint128 sub =
            static_cast<uint128>(b) +
            borrow;

        result =
            static_cast<uint64_t>(
                static_cast<uint128>(a) -
                sub
            );

        return static_cast<unsigned char>(
            static_cast<uint128>(a) < sub
        );
    }

    static uint64_t multiply64(
        uint64_t a,
        uint64_t b,
        uint64_t& high)
    {
        uint128 result =
            static_cast<uint128>(a) *
            static_cast<uint128>(b);

        high =
            static_cast<uint64_t>(
                result >> 64
            );

        return static_cast<uint64_t>(result);
    }

    std::string toHex() const
    {
        std::ostringstream stream;

        stream << std::hex
               << std::setfill('0');

        for (int i = 31; i >= 0; --i)
        {
            stream << std::setw(16)
                   << data[i];
        }

        std::string result =
            stream.str();

        std::size_t first =
            result.find_first_not_of('0');

        if (first == std::string::npos)
            return "0";

        return result.substr(first);
    }

public:
    uint2048() = default;

    uint2048(uint64_t value)
    {
        data[0] = value;
    }

    explicit uint2048(std::string hex)
    {
        if (hex.empty())
            return;

        std::size_t start = 0;

        if (hex.size() >= 2 &&
            hex[0] == '0' &&
            (hex[1] == 'x' || hex[1] == 'X'))
        {
            start = 2;
        }

        if (start == hex.size())
        {
            throw std::invalid_argument(
                "Invalid hexadecimal number"
            );
        }

        if (hex.size() - start > 512)
        {
            throw std::invalid_argument(
                "Number does not fit into uint2048"
            );
        }

        for (std::size_t i = start;
             i < hex.size();
             ++i)
        {
            hexValue(hex[i]);
        }

        std::size_t end =
            hex.size();

        for (int limb = 0;
             limb < 32 && end > start;
             ++limb)
        {
            std::size_t begin =
                (end >= start + 16)
                    ? end - 16
                    : start;

            uint64_t value = 0;

            for (std::size_t i = begin;
                 i < end;
                 ++i)
            {
                value <<= 4;
                value |= hexValue(hex[i]);
            }

            data[limb] = value;

            end = begin;
        }
    }

    static constexpr std::size_t BLOCK_SIZE = 240;

    static std::vector<uint2048> stringToBlocks(
        const std::string& text)
    {
        std::vector<uint2048> blocks;

        for (std::size_t pos = 0;
             pos < text.size();
             pos += BLOCK_SIZE)
        {
            std::size_t length =
                std::min(
                    BLOCK_SIZE,
                    text.size() - pos
                );

            std::string hex;

            hex.reserve(length * 2);

            for (std::size_t i = 0;
                 i < length;
                 ++i)
            {
                unsigned char c =
                    static_cast<unsigned char>(
                        text[pos + i]
                    );

                const char* digits =
                    "0123456789ABCDEF";

                hex += digits[c >> 4];
                hex += digits[c & 0x0F];
            }

            blocks.emplace_back(hex);
        }

        return blocks;
    }

    static std::string blocksToString(
        const std::vector<uint2048>& blocks)
    {
        std::string result;

        for (const auto& block : blocks)
        {
            std::string hex =
                block.toHex();

            if (hex.size() % 2 != 0)
                hex = "0" + hex;

            for (std::size_t i = 0;
                 i < hex.size();
                 i += 2)
            {
                unsigned int value =
                    std::stoul(
                        hex.substr(i, 2),
                        nullptr,
                        16
                    );

                result +=
                    static_cast<char>(value);
            }
        }

        return result;
    }

    uint64_t& operator[](std::size_t index)
    {
        return data[index];
    }

    const uint64_t& operator[](
        std::size_t index) const
    {
        return data[index];
    }

    uint2048 operator+(
        const uint2048& other) const
    {
        uint2048 result;

        unsigned char carry = 0;

        for (int i = 0; i < 32; ++i)
        {
            carry = addCarry(
                carry,
                data[i],
                other.data[i],
                result.data[i]
            );
        }

        return result;
    }

    uint2048 operator-(
        const uint2048& other) const
    {
        uint2048 result;

        unsigned char borrow = 0;

        for (int i = 0; i < 32; ++i)
        {
            borrow = subBorrow(
                borrow,
                data[i],
                other.data[i],
                result.data[i]
            );
        }

        return result;
    }

    bool operator==(
        const uint2048& other) const
    {
        for (int i = 0; i < 32; ++i)
        {
            if (data[i] != other.data[i])
                return false;
        }

        return true;
    }

    bool operator==(uint64_t other) const
    {
        if (data[0] != other)
            return false;

        for (int i = 1; i < 32; ++i)
        {
            if (data[i] != 0)
                return false;
        }

        return true;
    }

    std::strong_ordering operator<=>(
        const uint2048& other) const
    {
        for (int i = 31; i >= 0; --i)
        {
            if (data[i] < other.data[i])
                return std::strong_ordering::less;

            if (data[i] > other.data[i])
                return std::strong_ordering::greater;
        }

        return std::strong_ordering::equal;
    }

    uint2048 operator>>(int) const
    {
        uint2048 result;

        for (int i = 0; i < 31; ++i)
        {
            result.data[i] =
                (data[i] >> 1) |
                (data[i + 1] << 63);
        }

        result.data[31] =
            data[31] >> 1;

        return result;
    }

    uint2048 operator<<(int) const
    {
        uint2048 result;

        for (int i = 1; i < 32; ++i)
        {
            result.data[i] =
                (data[i] << 1) |
                (data[i - 1] >> 63);
        }

        result.data[0] =
            data[0] << 1;

        return result;
    }

    bool isOdd() const
    {
        return (data[0] & 1) != 0;
    }

    bool isZero() const
    {
        for (uint64_t value : data)
        {
            if (value != 0)
                return false;
        }

        return true;
    }

    bool getBit(int index) const
    {
        int block =
            index / 64;

        int offset =
            index % 64;

        return (
            (data[block] >> offset) &
            1ULL
        ) != 0;
    }

    uint2048 operator%(
        const uint2048& other) const
    {
        if (other.isZero())
        {
            throw std::invalid_argument(
                "Can't divide by 0"
            );
        }

        uint2048 remainder;

        for (int i = 2047; i >= 0; --i)
        {
            remainder =
                remainder << 1;

            if (getBit(i))
                remainder[0] |= 1;

            if (remainder >= other)
            {
                remainder =
                    remainder - other;
            }
        }

        return remainder;
    }

    uint2048 operator*(
        const uint2048& other) const
    {
        uint2048 result;

        for (int i = 0; i < 32; ++i)
        {
            uint64_t carry = 0;

            for (int j = 0;
                 i + j < 32;
                 ++j)
            {
                int index =
                    i + j;

                uint64_t high;

                uint64_t low =
                    multiply64(
                        data[i],
                        other.data[j],
                        high
                    );

                unsigned char c1 =
                    addCarry(
                        0,
                        result.data[index],
                        low,
                        result.data[index]
                    );

                unsigned char c2 =
                    addCarry(
                        0,
                        high,
                        carry,
                        high
                    );

                unsigned char c3 = 0;
                unsigned char c4 = 0;

                if (index + 1 < 32)
                {
                    c3 = addCarry(
                        0,
                        result.data[index + 1],
                        high,
                        result.data[index + 1]
                    );

                    c4 = addCarry(
                        0,
                        result.data[index + 1],
                        c1,
                        result.data[index + 1]
                    );
                }

                carry =
                    c2 | c3 | c4;
            }
        }

        return result;
    }

    uint2048 operator/(
        const uint2048& other) const
    {
        if (other.isZero())
        {
            throw std::invalid_argument(
                "Can't divide by 0"
            );
        }

        uint2048 result;
        uint2048 remainder;

        for (int i = 2047; i >= 0; --i)
        {
            remainder =
                remainder << 1;

            if (getBit(i))
                remainder[0] |= 1;

            if (remainder >= other)
            {
                remainder =
                    remainder - other;

                result[i / 64] |=
                    (uint64_t(1) << (i % 64));
            }
        }

        return result;
    }

    uint2048 addMod(
        const uint2048& a,
        const uint2048& b,
        const uint2048& m) const
    {
        uint2048 result;

        unsigned char carry = 0;

        for (int i = 0; i < 32; ++i)
        {
            carry = addCarry(
                carry,
                a[i],
                b[i],
                result[i]
            );
        }

        if (!carry)
        {
            if (result >= m)
                result =
                    result - m;

            return result;
        }

        uint2048 mb =
            m - b;

        if (a >= mb)
            return a - mb;

        return a - mb + m;
    }

    uint2048 mulMod(
        uint2048 a,
        uint2048 b,
        const uint2048& m)
    {
        uint2048 result(0);

        while (b != uint2048(0))
        {
            if (b.isOdd())
            {
                result =
                    addMod(
                        result,
                        a,
                        m
                    );
            }

            a =
                addMod(
                    a,
                    a,
                    m
                );

            b =
                b >> 1;
        }

        return result;
    }

    uint2048 modPow(
        uint2048 a,
        uint2048 b,
        const uint2048& m)
    {
        a = a % m;

        uint2048 result(1);

        while (!b.isZero())
        {
            if (b.isOdd())
            {
                result =
                    mulMod(
                        result,
                        a,
                        m
                    );
            }

            a =
                mulMod(
                    a,
                    a,
                    m
                );

            b =
                b >> 1;
        }

        return result;
    }
};
