using System.ComponentModel.DataAnnotations;

namespace FMFlow.Entities;

public enum NonceType
{
	Unassigned,
	VerifyIntegrationStatus,
	HandleOutlookIntegration,
	HandleGoogleIntegration,
	PasswordReset,
	CustomerActivation,
	CustomerMagicLink,
	EstimateRecipientMagicLink,
}
