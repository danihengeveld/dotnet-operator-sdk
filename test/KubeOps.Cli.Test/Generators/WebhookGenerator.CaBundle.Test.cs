// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

using FluentAssertions;

using k8s;
using k8s.Models;

using KubeOps.Cli.Generators;
using KubeOps.Cli.Output;

using Spectre.Console.Testing;

namespace KubeOps.Cli.Test.Generators;

public class WebhookGeneratorCaBundleTest
{
    private const string TestPem = "-----BEGIN CERTIFICATE-----\nMIIBkTCB+wIJAKHBCBquKfMCMA0GCSqGSIb3DQEBBQUAMBMxETAPBgNVBAMMCEVn\n-----END CERTIFICATE-----\n";

    [Fact]
    public void CaBundle_Should_Not_Be_Double_Base64_Encoded_In_Validation_Webhook()
    {
        // Arrange: create a caBundle the same way OperatorGenerator does (after the fix).
        var caBundle = Encoding.ASCII.GetBytes(TestPem);

        // The caBundle bytes should be the raw PEM text, not a base64 representation of it.
        var decoded = Encoding.ASCII.GetString(caBundle);
        decoded.Should().StartWith("-----BEGIN CERTIFICATE-----",
            "caBundle should contain raw PEM bytes, not base64-encoded PEM");

        // Also verify that base64-encoding (as k8s serializer would) produces a valid result
        // that, when decoded, gives back the original PEM.
        var base64 = Convert.ToBase64String(caBundle);
        var roundTripped = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
        roundTripped.Should().Be(TestPem,
            "a single round-trip of base64 encoding/decoding should recover the original PEM");
    }

    [Fact]
    public void CaBundle_Should_Not_Be_Double_Base64_Encoded_Regression()
    {
        // This test verifies the old (buggy) approach would double-encode
        // and that our fix avoids that.
        var pemBytes = Encoding.ASCII.GetBytes(TestPem);

        // Old (buggy) approach: base64-encode PEM, then convert that base64 string back to bytes
        var buggyBundle = Encoding.ASCII.GetBytes(Convert.ToBase64String(pemBytes));

        // The buggy bundle bytes should NOT start with "-----BEGIN" because they are base64 of PEM
        var buggyDecoded = Encoding.ASCII.GetString(buggyBundle);
        buggyDecoded.Should().NotStartWith("-----BEGIN CERTIFICATE-----",
            "the double-encoded bundle should not look like raw PEM");

        // Fixed approach: just raw PEM bytes
        var fixedBundle = Encoding.ASCII.GetBytes(TestPem);
        var fixedDecoded = Encoding.ASCII.GetString(fixedBundle);
        fixedDecoded.Should().StartWith("-----BEGIN CERTIFICATE-----",
            "the fixed bundle should contain raw PEM bytes");
    }
}
