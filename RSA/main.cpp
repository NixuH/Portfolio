#include <algorithm>
#include <chrono>
#include <iomanip>
#include <iostream>
#include <numeric>
#include <stdexcept>
#include <string>
#include <vector>
#include <limits>

#include "CommunicationHandler.h"
#include "uint2048.h"
#include "RSA2048.h"
#include "RhoRSACracker.h"

using namespace std;

struct testValue
{
    uint2048 p;
    uint2048 q;
    unsigned int bits;
};

constexpr std::size_t UINT2048_BYTES = 256;
constexpr std::size_t UINT2048_LIMBS = 32;

std::vector<uint8_t> uint2048ToBytes(const uint2048& value)
{
    std::vector<uint8_t> bytes(UINT2048_BYTES);

    for (std::size_t i = 0; i < UINT2048_LIMBS; ++i)
    {
        uint64_t limb = value[i];

        for (std::size_t j = 0; j < sizeof(uint64_t); ++j)
        {
            bytes[i * 8 + j] =
                static_cast<uint8_t>(limb >> (j * 8));
        }
    }

    return bytes;
}

uint2048 bytesToUint2048(const std::vector<uint8_t>& bytes)
{
    if (bytes.size() != UINT2048_BYTES)
        throw runtime_error(
            "Invalid uint2048 size"
        );

    uint2048 value(0);

    for (std::size_t i = 0; i < UINT2048_LIMBS; ++i)
    {
        uint64_t limb = 0;

        for (std::size_t j = 0; j < sizeof(uint64_t); ++j)
        {
            limb |=
                static_cast<uint64_t>(
                    bytes[i * 8 + j]
                ) << (j * 8);
        }

        value[i] = limb;
    }

    return value;
}

vector<uint8_t> stringToBytes(const string& value)
{
    return vector<uint8_t>(
        value.begin(),
        value.end()
    );
}

string bytesToString(const vector<uint8_t>& value)
{
    return string(
        value.begin(),
        value.end()
    );
}

int main()
{
    CommunicationHandler::initialize();

    while (true) {
        string ans;
        cout
            << "what do you want to do?\n"
            << "1 - speedtest\n"
            << "2 - RSA test\n"
            << "3 - run in server mode\n"
            << "4 - run in client mode\n"
            << "other answer to exit\n";
                cin >> ans;

        if (ans == "1") {
            vector<testValue> tests =
            {
                // 8-bit factors
                { 251,  241,  8 },
                { 251,  239,  8 },
                { 251,  233,  8 },
                { 251,  229,  8 },
                { 251,  227,  8 },
                { 241,  239,  8 },
                { 241,  233,  8 },
                { 239,  229,  8 },
                { 233,  227,  8 },
                { 229,  223,  8 },

                // 12-bit factors
                { 4093, 4091, 12 },
                { 4093, 4079, 12 },
                { 4093, 4073, 12 },
                { 4093, 4057, 12 },
                { 4093, 4051, 12 },
                { 4091, 4079, 12 },
                { 4091, 4073, 12 },
                { 4079, 4057, 12 },
                { 4073, 4051, 12 },
                { 4057, 4049, 12 },

                // 16-bit factors
                { 65521, 65497, 16 },
                { 65521, 65479, 16 },
                { 65521, 65449, 16 },
                { 65521, 65437, 16 },
                { 65521, 65423, 16 },
                { 65497, 65479, 16 },
                { 65497, 65449, 16 },
                { 65479, 65437, 16 },
                { 65449, 65423, 16 },
                { 65437, 65413, 16 },

                // 24-bit factors
                { 16777213, 16777199, 24 },
                { 16777213, 16777183, 24 },
                { 16777213, 16777153, 24 },
                { 16777213, 16777141, 24 },
                { 16777213, 16777139, 24 },
                { 16777199, 16777183, 24 },
                { 16777199, 16777153, 24 },
                { 16777183, 16777141, 24 },
                { 16777153, 16777139, 24 },
                { 16777141, 16777127, 24 },

                // 32-bit factors
                { 4294967291ULL, 4294967279ULL, 32 },
                { 4294967291ULL, 4294967231ULL, 32 },
                { 4294967291ULL, 4294967197ULL, 32 },
                { 4294967279ULL, 4294967231ULL, 32 },
                { 4294967279ULL, 4294967197ULL, 32 },
                { 4294967231ULL, 4294967197ULL, 32 },
                { 4294967197ULL, 4294967189ULL, 32 },
                { 4294967189ULL, 4294967161ULL, 32 },
                { 4294967161ULL, 4294967143ULL, 32 },
                { 4294967143ULL, 4294967087ULL, 32 },

                // 33-bit factors
                { 8589934583ULL, 8589934567ULL, 33 },
                { 8589934583ULL, 8589934543ULL, 33 },
                { 8589934583ULL, 8589934513ULL, 33 },
                { 8589934583ULL, 8589934487ULL, 33 },
                { 8589934583ULL, 8589934307ULL, 33 },
                { 8589934567ULL, 8589934543ULL, 33 },
                { 8589934567ULL, 8589934513ULL, 33 },
                { 8589934543ULL, 8589934487ULL, 33 },
                { 8589934513ULL, 8589934307ULL, 33 },
                { 8589934487ULL, 8589934291ULL, 33 }
            };

            constexpr int runsPerTest = 20;
            constexpr int worstRuns = 5;

            cout << fixed << setprecision(2);

            for (unsigned int bits : { 8u, 12u, 16u, 24u, 32u, 33u })
            {
                cout << bits*2 << "-BIT RSA\n";

                cout<< "Wanna run this speed test? (y/n)";
                string answer;
                cin >> answer;
                if (answer != "y" && answer != "Y")
                    break;

                vector<double> allAverages;
                vector<double> allWorstAverages;

                int testCount = 0;
                for (const auto& test : tests)
                {
                    if (test.bits != bits)
                        continue;

                    ++testCount;

                    uint2048 p = test.p;
                    uint2048 q = test.q;

                    RSA2048::Key publicKey =
                        RSA2048::createPublicKey(p, q);

                    vector<double> times;
                    times.reserve(runsPerTest);

                    for (int run = 0; run < runsPerTest; ++run)
                    {
                        auto start =
                            chrono::steady_clock::now();

                        auto factors =
                            RhoRSACracker::factorParallel(
                                publicKey.first,
                                20
                            );

                        auto end =
                            chrono::steady_clock::now();

                        double time =
                            static_cast<double>(
                                chrono::duration_cast<
                                    chrono::microseconds
                                >(end - start).count()
                            );

                        if (
                            !(
                                (factors.first == p &&
                                 factors.second == q)
                                ||
                                (factors.first == q &&
                                 factors.second == p)
                            )
                        )
                        {
                            throw runtime_error(
                                "Wrong factorization result"
                            );
                        }

                        times.push_back(time);
                    }

                    sort(
                        times.rbegin(),
                        times.rend()
                    );

                    double average =
                        accumulate(
                            times.begin(),
                            times.end(),
                            0.0
                        ) / times.size();

                    double worstAverage =
                        accumulate(
                            times.begin(),
                            times.size() >= 5 ? times.begin() + worstRuns: times.end(),
                            0.0
                        ) / (times.size() >= 5 ? worstRuns : (times.end() - times.begin()));

                    allAverages.push_back(average);
                    allWorstAverages.push_back(worstAverage);


                    cout
                        << "  average:       "
                        << average
                        << " us\n";

                    cout
                        << "  worst:         "
                        << times.front()
                        << " us\n";

                    cout
                        << "  worst "
                        << " average: "
                        << worstAverage
                        << " us\n\n";
                }

                double average =
                    accumulate(
                        allAverages.begin(),
                        allAverages.end(),
                        0.0
                    ) / allAverages.size();

                double worstAverage =
                    accumulate(
                        allWorstAverages.begin(),
                        allWorstAverages.end(),
                        0.0
                    ) / allWorstAverages.size();

                cout << "\n";

                cout
                    << bits*2
                    << "-bit overall average: "
                    << average
                    << " us\n";

                cout
                    << bits*2
                    << "-bit average of worst runs: "
                    << worstAverage
                    << " us\n";

                cout << "\n";
            }

        }
        else if (ans == "2") {
            uint2048 p(
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF97"
                );

            uint2048 q(
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC1F"
                );

            auto publicKey = RSA2048::createPublicKey(p, q);
            cout << "factors test passed!\n\n";
            auto privateKey = RSA2048::createPrivateKey(publicKey, p, q);

            auto c = 255;

            std::string text= "Hello World!";

            cout << "text: \n";
            cout << text << "\n\n";
            auto encrypted = RSA2048::encryptString(text, publicKey);

            cout << "Encrypted: \n\n";
            for (auto i : encrypted) {
                for (int j=31; j >= 0; --j) {
                    cout << i[j];
                }
            }
            cout << "\n";
            cout << "\n\nDecrypted: \n";

            auto decrypted = RSA2048::decryptString(encrypted, privateKey);
            cout << decrypted<<"\n";

            if (decrypted != text) {
                cout<< "Error!\n";
            }
            else {
                cout<< "Test passed!\n";
            }
        }
        else if (ans == "3") {
            CommunicationHandler communication;

            cout << "Enter port: ";

            uint16_t port;
            cin >> port;

            uint2048 p(
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF97"
                );

            uint2048 q(
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC1F"
                );


            auto publicKey =
                RSA2048::createPublicKey(p, q);

            auto privateKey =
                RSA2048::createPrivateKey(
                    publicKey,
                    p,
                    q
                );
            cout << "Waiting for client...\n";

            communication.listenOn(port);

            cout << "Client connected!\n";

            vector<uint8_t> hello =
                communication.receive();

            if (bytesToString(hello) != "hello")
            {
                cout << "Invalid handshake.\n";
                continue;
            }

            cout << "Received hello.\n";

            communication.send(
                uint2048ToBytes(publicKey.first)
            );

            communication.send(
                uint2048ToBytes(publicKey.second)
            );

            cout << "Public key sent.\n";

            vector<uint8_t> received =
                communication.receive();

            if (bytesToString(received) != "received")
            {
                cout << "Invalid confirmation.\n";
                continue;
            }

            cout << "Secure connection established!\n";

            while (true) {
                vector<uint8_t> encryptedBytes = communication.receive();

                if (bytesToString(encryptedBytes) == "disconnect")
                {
                    cout << "Client disconnected.\n";
                    break;
                }

                if (
                    encryptedBytes.size() %
                    UINT2048_BYTES != 0
                )
                {
                    throw runtime_error(
                        "Invalid encrypted message size"
                    );
                }

                vector<uint2048> encrypted;

                encrypted.reserve(
                    encryptedBytes.size() /
                    UINT2048_BYTES
                );

                for (
                    std::size_t i = 0;
                    i < encryptedBytes.size();
                    i += UINT2048_BYTES
                )
                {
                    vector<uint8_t> block(
                        encryptedBytes.begin() + i,
                        encryptedBytes.begin() +
                            i + UINT2048_BYTES
                    );

                    encrypted.push_back(
                        bytesToUint2048(block)
                    );
                }

                cout
                    << "Client: \n"
                    <<"got: \n";

                cout << string(
                encryptedBytes.begin(),
                    encryptedBytes.end()
                );

                string decrypted =
                    RSA2048::decryptString(
                        encrypted,
                        privateKey
                    );

                cout << '\n';

                cout
                    << "Total bytes: "
                    << encryptedBytes.size()
                    << '\n';

                cout
                    << "Decrypted:\n"
                    << decrypted
                    << '\n';

            }
        }
        else if (ans == "4") {
            CommunicationHandler communication;

            string ip;

            cout << "Enter server IP: ";
            cin >> ip;

            cout << "Enter port: ";

            uint16_t port;
            cin >> port;

            communication.connectTo(
                ip,
                port
            );

            cout << "Connected to server.\n";

            communication.send(
                stringToBytes("hello")
            );

            vector<uint8_t> nBytes =
                communication.receive();

            vector<uint8_t> eBytes =
                communication.receive();

            uint2048 n =
                bytesToUint2048(nBytes);

            uint2048 e =
                bytesToUint2048(eBytes);

            RSA2048::Key publicKey = {
                n,
                e
            };

            cout << "Public key received.\n";

            communication.send(
                stringToBytes("received")
            );

            cout
                << "Secure connection established!\n";

            cin.ignore(
                numeric_limits<streamsize>::max(),
                '\n'
            );

            while (true)
            {
                string text;

                cout << "> ";
                getline(cin, text);

                if (text == "quit")
                {
                    communication.send(
                        stringToBytes("disconnect")
                    );

                    break;
                }

                vector<uint2048> encrypted =
                    RSA2048::encryptString(
                        text,
                        publicKey
                    );

                vector<uint8_t> encryptedBytes;

                encryptedBytes.reserve(
                    encrypted.size() *
                    UINT2048_BYTES
                );

                for (const uint2048& block : encrypted)
                {
                    vector<uint8_t> bytes =
                        uint2048ToBytes(block);

                    encryptedBytes.insert(
                        encryptedBytes.end(),
                        bytes.begin(),
                        bytes.end()
                    );
                }

                communication.send(
                    encryptedBytes
                );
                cout << "Sent (" << encryptedBytes.size() << " bytes):\n";

                cout << string(
                encryptedBytes.begin(),
                    encryptedBytes.end()
                );

                cout << '\n';
            }
        }
        else {
            break;
        }
    }

    CommunicationHandler::cleanup();
    return 0;
}
