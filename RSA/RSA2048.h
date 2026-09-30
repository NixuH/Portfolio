#pragma once

#include <cassert>
#include <cstdint>
#include <iostream>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

#include "uint2048.h"

class RSA2048
{
    private:

    static uint2048 gcd(
        uint2048 a,
        uint2048 b)
    {
        while (!b.isZero())
        {
            uint2048 remainder = a % b;

            a = b;
            b = remainder;
        }

        return a;
    }

    static uint2048 modInverse(
        const uint2048& e,
        const uint2048& phi)
    {
        uint2048 oldR = e;
        uint2048 r = phi;

        uint2048 oldT(1);
        uint2048 t(0);

        while (!r.isZero())
        {
            uint2048 quotient = oldR / r;
            uint2048 remainder = oldR % r;

            oldR = r;
            r = remainder;

            uint2048 product =
                mulMod(
                    quotient,
                    t,
                    phi
                );

            if (oldT >= product)
            {
                oldT = oldT - product;
            }
            else
            {
                oldT =
                    oldT +
                    phi -
                    product;
            }

            uint2048 temp = t;
            t = oldT;
            oldT = temp;
        }

        assert(oldR == uint2048(1));

        return oldT;
    }

    static uint2048 mulMod(
        uint2048 a,
        uint2048 b,
        const uint2048& m)
    {
        uint2048 result(0);

        a = a % m;

        while (!b.isZero())
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

            b = b >> 1;
        }

        return result;
    }

    static uint2048 publicKeyPower(
        uint2048 a,
        uint2048 b,
        const uint2048& m)
    {
        uint2048 result(1);

        a = a % m;

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

            b = b >> 1;
        }

        return result;
    }

    static uint2048 addMod(
        const uint2048& a,
        const uint2048& b,
        const uint2048& m)
    {
        if (a >= m - b)
            return a - (m - b);

        return a + b;
    }

    static bool isPrime(const uint2048& n)
    {
        if (n < uint2048(2))
            return false;

        if (n == uint2048(2))
            return true;

        if (!n.isOdd())
            return false;

        uint2048 d = n - uint2048(1);
        unsigned int s = 0;

        while (!d.isOdd())
        {
            d = d >> 1;
            ++s;
        }


        const uint64_t bases[] =
        {
            2,
            3,
            5,
            7,
            11,
            13,
            17,
            19
        };

        for (uint64_t base : bases)
        {
            uint2048 a(base);

            if (a >= n)
                continue;

            uint2048 x =
                publicKeyPower(
                    a,
                    d,
                    n
                );

            if (x == uint2048(1) ||
                x == n - uint2048(1))
            {
                continue;
            }

            bool prime = false;

            for (unsigned int r = 1; r < s; ++r)
            {
                x =
                    mulMod(
                        x,
                        x,
                        n
                    );

                if (x == n - uint2048(1))
                {
                    prime = true;
                    break;
                }
            }

            if (!prime)
                return false;
        }

        return true;
    }

public:
    using Key = std::pair<uint2048, uint2048>;

    static Key createPublicKey(
        const uint2048& p,
        const uint2048& q)
    {
        if (p.isZero() || q.isZero())
            throw std::invalid_argument(
                "p and q must be non-zero"
            );

        if (p == q)
            throw std::invalid_argument(
                "p and q must be different"
            );

        if (!isPrime(p))
            throw std::invalid_argument(
                "p is not prime"
            );

        if (!isPrime(q))
            throw std::invalid_argument(
                "q is not prime"
            );

        uint2048 n = p * q;

        uint2048 phi =
            (p - uint2048(1)) *
            (q - uint2048(1));

        uint2048 e(65537);

        while (gcd(phi, e) != uint2048(1))
            e = e + uint2048(1);

        return { n, e };
    }

    static Key createPrivateKey(
        const Key& publicKey,
        const uint2048& p,
        const uint2048& q)
    {
        uint2048 phi =
            (p - uint2048(1)) *
            (q - uint2048(1));

        if (gcd(publicKey.second, phi) != uint2048(1))
        {
            throw std::invalid_argument(
                "e is not coprime with phi"
            );
        }

        uint2048 d =
            modInverse(
                publicKey.second,
                phi
            );

        return {
            publicKey.first,
            d
        };
    }

    static uint2048 encrypt(
        const uint2048& value,
        const Key& publicKey)
    {
        assert(value < publicKey.first);

        return publicKeyPower(
            value,
            publicKey.second,
            publicKey.first
        );
    }

    static uint2048 decrypt(
        const uint2048& value,
        const Key& privateKey)
    {
        return publicKeyPower(
            value,
            privateKey.second,
            privateKey.first
        );
    }

    static std::vector<uint2048> encryptString(
        const std::string& text,
        const Key& publicKey)
    {
        std::vector<uint2048> blocks =
            uint2048::stringToBlocks(text);

        for (auto& block : blocks)
            block = encrypt(block, publicKey);

        return blocks;
    }

    static std::string decryptString(
        const std::vector<uint2048>& encryptedBlocks,
        const Key& privateKey)
    {
        std::vector<uint2048> blocks;
        blocks.reserve(encryptedBlocks.size());

        for (const auto& encryptedBlock : encryptedBlocks)
        {
            blocks.push_back(
                decrypt(
                    encryptedBlock,
                    privateKey
                )
            );
        }

        return uint2048::blocksToString(blocks);
    }

};