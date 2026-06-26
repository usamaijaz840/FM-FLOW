namespace FMFlow.Pro.Interface.Enums;

/// <summary>
/// The possible onboarding form stop values from the UI
/// </summary>
public enum OnboardingFormStop
{
    completed,
    setSelfDescriptionFormToggle,
    setReferralSourceToggle,
    setPaymentFormToggle,
    setDocumentUploadToggle,
    setBusinessInformationFormToggle,
    setBusinessProfileFormToggle
}

/// <summary>
/// The internal states used for validation and processing
/// </summary>
public enum SavingState
{
    BusinessInformation,
    BusinessProfile,
    Completed,
    PersonalInformation,
    SelfDescription,
    ReferralSource,
    Payment
}

/// <summary>
/// Provides utility methods for onboarding stages
/// </summary>
public static class OnboardingStages
{
    /// <summary>
    /// Maps an onboarding form stop string to a SavingState
    /// </summary>
    /// <param name="formStop">The form stop string from the UI</param>
    /// <returns>The corresponding SavingState</returns>
    public static SavingState MapOnboardingFormStopToStage(string? formStop)
    {
        if (string.IsNullOrWhiteSpace(formStop))
            throw new ArgumentException("OnboardingFormStop is required.", nameof(formStop));

        if (!Enum.TryParse<OnboardingFormStop>(formStop, ignoreCase: true, out var parsedStop))
            throw new ArgumentException($"Invalid OnboardingFormStop value: {formStop}", nameof(formStop));

        return MapOnboardingFormStopToStage(parsedStop);
    }

    /// <summary>
    /// Maps an OnboardingFormStop enum value to a SavingState
    /// </summary>
    /// <param name="formStop">The form stop enum value</param>
    /// <returns>The corresponding SavingState</returns>
    public static SavingState MapOnboardingFormStopToStage(OnboardingFormStop? formStop)
    {
        return formStop switch
        {
            OnboardingFormStop.setSelfDescriptionFormToggle => SavingState.BusinessInformation,
            OnboardingFormStop.setBusinessProfileFormToggle => SavingState.Payment,
            OnboardingFormStop.setReferralSourceToggle => SavingState.BusinessProfile,
            OnboardingFormStop.setPaymentFormToggle => SavingState.PersonalInformation,
            OnboardingFormStop.setDocumentUploadToggle => SavingState.SelfDescription,
            OnboardingFormStop.setBusinessInformationFormToggle => SavingState.ReferralSource,
            OnboardingFormStop.completed => SavingState.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(formStop), formStop, null)
        };
    }

    /// <summary>
    /// Determines if a stage is at or beyond a required stage in the progression
    /// </summary>
    /// <param name="currentStage">The current stage</param>
    /// <param name="requiredStage">The stage to check against</param>
    /// <returns>True if the current stage is at or beyond the required stage</returns>
    public static bool IsStageAtOrBeyond(SavingState currentStage, SavingState requiredStage)
    {
        int currentStageOrder = GetStageOrder(currentStage);
        int requiredStageOrder = GetStageOrder(requiredStage);

        return currentStageOrder >= requiredStageOrder;
    }

    /// <summary>
    /// Gets the sequential order of a stage in the progression
    /// </summary>
    private static int GetStageOrder(SavingState stage)
    {
        return stage switch
        {
            SavingState.PersonalInformation => 1,
            SavingState.BusinessProfile => 2,
            SavingState.ReferralSource => 3,
            SavingState.BusinessInformation => 4,
            SavingState.SelfDescription => 5,
            SavingState.Payment => 6,
            SavingState.Completed => 7,
            _ => 0
        };
    }
} 