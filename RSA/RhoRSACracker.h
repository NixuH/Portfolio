#pragma once

#include "uint2048.h"
#include "RSA2048.h"

#include <thread>
#include <vector>
#include <atomic>
#include <mutex>
#include <random>
#include <utility>
#include <functional>

class RhoRSACracker
{
public:
    static RSA2048::Key crack(
        const RSA2048::Key& publicKey,
        unsigned int numThreads =
            std::thread::hardware_concurrency())
    {
        if (numThreads == 0)
            numThreads = 4;


        auto [p, q] =
            factorParallel(
                publicKey.first,
                numThreads
            );


        return RSA2048::createPrivateKey(
            publicKey,
            p,
            q
        );
    }

    static std::pair<uint2048, uint2048> factorParallel(
        const uint2048& n,
        unsigned int numThreads)
    {
        if (!n.isOdd())
            return {
                uint2048(2),
                n / uint2048(2)
            };

        std::atomic<bool> found{ false };

        std::pair<uint2048, uint2048> result{
            uint2048(0),
            uint2048(0)
        };

        std::mutex resultMutex;

        std::vector<std::thread> threads;

        threads.reserve(numThreads);

        for (unsigned int i = 0;
             i < numThreads;
             ++i)
        {
            threads.emplace_back(
                &RhoRSACracker::worker,

                n,

                std::ref(found),
                std::ref(result),
                std::ref(resultMutex),

                i
            );
        }

        for (auto& thread : threads)
            thread.join();

        return result;
    }

private:
    static uint2048 gcd(
        uint2048 a,
        uint2048 b)
    {
        while (!b.isZero())
        {
            uint2048 temp =
                a % b;

            a = b;
            b = temp;
        }

        return a;
    }

    static uint2048 randomNumber(
        std::mt19937_64& rng)
    {
        uint2048 result;

        for (int i = 0; i < 32; ++i)
        {
            result[i] = rng();
        }

        return result;
    }

    static uint2048 randomNumberBelow(
        const uint2048& n,
        std::mt19937_64& rng)
    {
        while (true)
        {
            uint2048 value =
                randomNumber(rng);


            value =
                value % n;


            if (!value.isZero())
                return value;
        }
    }

    static uint2048 function(
        const uint2048& x,
        const uint2048& c,
        const uint2048& n)
    {
        uint2048 arithmetic;


        uint2048 square =
            arithmetic.mulMod(
                x,
                x,
                n
            );


        return arithmetic.addMod(
            square,
            c,
            n
        );
    }

    static uint2048 pollardRho(
        const uint2048& n,
        std::mt19937_64& rng)
    {

        if (!n.isOdd())
            return uint2048(2);

        while (true)
        {
            uint2048 x =
                randomNumberBelow(
                    n,
                    rng
                );

            uint2048 y = x;

            uint2048 c =
                randomNumberBelow(
                    n,
                    rng
                );

            uint2048 d(1);

            while (d == uint2048(1))
            {

                x =
                    function(
                        x,
                        c,
                        n
                    );

                y =
                    function(
                        y,
                        c,
                        n
                    );

                y =
                    function(
                        y,
                        c,
                        n
                    );

                uint2048 difference;

                if (x >= y)
                {
                    difference =
                        x - y;
                }
                else
                {
                    difference =
                        y - x;
                }

                d =
                    gcd(
                        difference,
                        n
                    );

                if (
                    d > uint2048(1) &&
                    d < n
                )
                {
                    return d;
                }
            }

        }
    }


    static void worker(
        const uint2048& n,
        std::atomic<bool>& found,
        std::pair<uint2048, uint2048>& result,
        std::mutex& resultMutex,
        unsigned int threadId)
    {
        std::random_device rd;

        std::seed_seq seed{
            rd(),
            rd(),
            threadId,

            static_cast<unsigned int>(
                std::hash<std::thread::id>{}(
                    std::this_thread::get_id()
                )
            )
        };

        std::mt19937_64 rng(seed);

        while (
            !found.load(
                std::memory_order_relaxed
            ))
        {
            uint2048 d =
                pollardRho(
                    n,
                    rng
                );

            if (
                d == uint2048(1) ||
                d == n
            )
            {
                continue;
            }

            uint2048 q =
                n / d;

            uint2048 arithmetic;

            if (
                arithmetic.mulMod(
                    d,
                    q,
                    n
                ) != uint2048(0)
            )
            {
                continue;
            }

            std::lock_guard<std::mutex> lock(
                resultMutex
            );

            if (!found.exchange(true))
            {
                result = {
                    d,
                    q
                };
            }

            return;
        }
    }
};