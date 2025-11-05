#!/usr/bin/env dotnet-script
#r "nuget: Konscious.Security.Cryptography.Argon2, 1.3.1"

using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

const int SaltSize = 16;
const int HashSize = 32;
const int Iterations = 4;
const int MemorySize = 65536;
const int DegreeOfParallelism = 1;

string HashPassword(string password)
{
    var salt = new byte[SaltSize];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(salt);
    }

    using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
    {
        Salt = salt,
        DegreeOfParallelism = DegreeOfParallelism,
        MemorySize = MemorySize,
        Iterations = Iterations
    };

    var hash = argon2.GetBytes(HashSize);

    var hashBytes = new byte[SaltSize + HashSize];
    Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
    Buffer.BlockCopy(hash, 0, hashBytes, SaltSize, HashSize);

    return Convert.ToBase64String(hashBytes);
}

Console.WriteLine(HashPassword("Admin@123"));
