using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.MraEis
{
    /// <summary>
    /// Configuration for connecting to the MRA EIS API directly.
    ///
    /// Terminal activation was already completed by the team - this app
    /// does not call activate-terminal itself. The token is used as-is
    /// until it expires or is rejected.
    ///
    /// CONFIRMED from official MRA docs (eis-api.mra.mw/docs/authorization.htm,
    /// x_access_key.htm, x_signature.htm):
    ///   - Authorization: Bearer {jwtToken} is the ONLY header required for
    ///     sales submission and all endpoints after activation+confirmation.
    ///   - x-access-key and x-signature are required ONLY during the terminal
    ///     activation and activation-confirmation steps, which are already
    ///     complete. They are not needed for ongoing sales submission.
    ///
    /// SecretKey and Signature are stored but not used in any current
    /// request - they relate to offline-mode QR signature generation,
    /// which is out of scope until offline support is built.
    /// </summary>
    /// <summary>
    /// Configuration for connecting to the MRA EIS API directly.
    ///
    /// Terminal activation was already completed by the team - this app
    /// does not call activate-terminal itself. The token is used as-is
    /// until it expires or is rejected.
    ///
    /// CONFIRMED from official MRA docs (eis-api.mra.mw/docs/authorization.htm,
    /// x_access_key.htm, x_signature.htm):
    ///   - Authorization: Bearer {Token} is the ONLY header required for
    ///     sales submission and all endpoints after activation+confirmation.
    ///   - x-access-key and x-signature are required ONLY during the terminal
    ///     activation and activation-confirmation steps, which are already
    ///     complete. They are not needed for ongoing sales submission.
    ///
    /// SecretKey and Signature are stored but not used in any current
    /// request - they relate to offline-mode QR signature generation,
    /// which is out of scope until offline support is built.
    /// </summary>
    public class MraEisConfig
    {
        public required string BaseUrl { get; init; }

        public required string TerminalId { get; init; }

        /// <summary>
        /// The Authorization Token (jwtToken) obtained from a prior activation.
        /// Used as-is: "Authorization: Bearer {Token}"
        /// </summary>
        public required string Token { get; init; }

        /// <summary>Stored but not currently used. See class remarks.</summary>
        public string? SecretKey { get; init; }

        /// <summary>Stored but not currently used. See class remarks.</summary>
        public string? Signature { get; init; }

        /// <summary>Stored but not currently used. See class remarks.</summary>
        public string? ActivationKey { get; init; }

        public int TimeoutSeconds { get; init; } = 30;
    }
}
