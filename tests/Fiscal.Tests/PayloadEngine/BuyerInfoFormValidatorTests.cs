using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.PayloadEngine
{
    public class BuyerInfoFormValidatorTests
    {
        private static BuyerInfoFormConfig BuildConfig() => new()
        {
            TriggerLabel = "B2B Sale",
            Fields =
            [
                new BuyerInfoField
            {
                Key      = "BuyerTaxNumber",
                Label    = "Tax Number",
                Required = true,
                Min      = 9,
                Max      = 9
            },
            new BuyerInfoField
            {
                Key      = "BuyerName",
                Label    = "Company Name",
                Required = true
            },
            new BuyerInfoField
            {
                Key      = "BuyerAddress",
                Label    = "Address",
                Required = false
            }
            ]
        };

        [Fact]
        public void ValidInput_ReturnsNoErrors()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "123456789",
                ["BuyerName"] = "Acme Corp",
                ["BuyerAddress"] = "123 Main St"
            };

            var errors = validator.Validate(values);

            Assert.Empty(errors);
        }

        [Fact]
        public void MissingRequiredField_ReturnsError()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "123456789",
                ["BuyerName"] = "",           // required but empty
                ["BuyerAddress"] = null
            };

            var errors = validator.Validate(values);

            Assert.True(errors.ContainsKey("BuyerName"));
            Assert.Contains("required", errors["BuyerName"],
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TaxNumberTooShort_ReturnsExactLengthError()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "12345",      // 5 chars, needs exactly 9
                ["BuyerName"] = "Acme Corp"
            };

            var errors = validator.Validate(values);

            Assert.True(errors.ContainsKey("BuyerTaxNumber"));
            Assert.Contains("exactly 9", errors["BuyerTaxNumber"],
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TaxNumberTooLong_ReturnsExactLengthError()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "1234567890",  // 10 chars, needs exactly 9
                ["BuyerName"] = "Acme Corp"
            };

            var errors = validator.Validate(values);

            Assert.True(errors.ContainsKey("BuyerTaxNumber"));
            Assert.Contains("exactly 9", errors["BuyerTaxNumber"],
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OptionalFieldEmpty_ReturnsNoError()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "123456789",
                ["BuyerName"] = "Acme Corp",
                ["BuyerAddress"] = ""             // optional, empty is fine
            };

            var errors = validator.Validate(values);

            Assert.False(errors.ContainsKey("BuyerAddress"));
        }

        [Fact]
        public void MultipleErrors_AllReturned()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "123",        // too short
                ["BuyerName"] = ""            // required but empty
            };

            var errors = validator.Validate(values);

            Assert.Equal(2, errors.Count);
            Assert.True(errors.ContainsKey("BuyerTaxNumber"));
            Assert.True(errors.ContainsKey("BuyerName"));
        }

        [Fact]
        public void ExactMinMaxLength_Passes()
        {
            var validator = new BuyerInfoFormValidator(BuildConfig());
            var values = new Dictionary<string, string?>
            {
                ["BuyerTaxNumber"] = "123456789",  // exactly 9 - should pass
                ["BuyerName"] = "Acme Corp"
            };

            var errors = validator.Validate(values);

            Assert.False(errors.ContainsKey("BuyerTaxNumber"));
        }
    }
}
