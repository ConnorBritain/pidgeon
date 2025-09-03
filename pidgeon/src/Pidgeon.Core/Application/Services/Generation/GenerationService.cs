// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Standards;
using Pidgeon.Core.Application.DTOs;

namespace Pidgeon.Core.Services;

internal class GenerationService : IGenerationService
{
    private readonly Generation.IGenerationService _domainGenerationService;
    private readonly IStandardPluginRegistry _pluginRegistry;
    
    public GenerationService(
        Generation.IGenerationService domainGenerationService,
        IStandardPluginRegistry pluginRegistry)
    {
        _domainGenerationService = domainGenerationService;
        _pluginRegistry = pluginRegistry;
    }
    
    public async Task<Result<IReadOnlyList<string>>> GenerateSyntheticDataAsync(string standard, string messageType, int count = 1, Generation.GenerationOptions? options = null)
    {
        try
        {
            var messages = new List<string>();
            var generationOptions = options ?? new Generation.GenerationOptions();
            
            for (int i = 0; i < count; i++)
            {
                var result = messageType.ToUpperInvariant() switch
                {
                    "PATIENT" => await GeneratePatientMessageAsync(standard, generationOptions),
                    "MEDICATION" => await GenerateMedicationMessageAsync(standard, generationOptions),
                    "PRESCRIPTION" => await GeneratePrescriptionMessageAsync(standard, generationOptions),
                    "ENCOUNTER" => await GenerateEncounterMessageAsync(standard, generationOptions),
                    "ADT" => await GenerateEncounterMessageAsync(standard, generationOptions), // ADT maps to encounter
                    "RDE" => await GeneratePrescriptionMessageAsync(standard, generationOptions), // RDE maps to prescription
                    _ => Result<string>.Failure($"Unsupported message type: {messageType}")
                };
                
                if (!result.IsSuccess)
                    return Result<IReadOnlyList<string>>.Failure(result.Error);
                    
                messages.Add(result.Value);
            }
            
            return Result<IReadOnlyList<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"Generation failed: {ex.Message}");
        }
    }
    
    private async Task<Result<string>> GeneratePatientMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        
        // Use plugin registry to find appropriate standard plugin
        var plugin = _pluginRegistry.GetPlugin(standard);
        if (plugin != null)
        {
            var messageOptions = new MessageOptions
            {
                Timestamp = DateTime.UtcNow,
                SendingApplication = "PIDGEON",
                ReceivingApplication = "UNKNOWN"
            };

            var admissionResult = plugin.MessageFactory.CreatePatientAdmission(patient.ToDto(), messageOptions);
            if (admissionResult.IsSuccess)
            {
                var serializationResult = admissionResult.Value.Serialize();
                if (serializationResult.IsSuccess)
                {
                    return Result<string>.Success(serializationResult.Value);
                }
                else
                {
                    return Result<string>.Failure($"Failed to serialize patient message: {serializationResult.Error}");
                }
            }
            else
            {
                return Result<string>.Failure($"Failed to create patient admission message: {admissionResult.Error}");
            }
        }
        
        // Fallback to human-readable for unsupported standards
        return Result<string>.Success($"Patient: {patient.Name.DisplayName}, DOB: {patient.BirthDate:yyyy-MM-dd}");
    }
    
    private async Task<Result<string>> GenerateMedicationMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (!medicationResult.IsSuccess)
            return Result<string>.Failure(medicationResult.Error);
            
        var medication = medicationResult.Value;
        return Result<string>.Success($"Medication: {medication.DisplayName} ({medication.GenericName})");
    }
    
    private async Task<Result<string>> GeneratePrescriptionMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        
        // Use plugin registry to find appropriate standard plugin
        var plugin = _pluginRegistry.GetPlugin(standard);
        if (plugin != null)
        {
            var messageOptions = new MessageOptions
            {
                Timestamp = DateTime.UtcNow,
                SendingApplication = "PIDGEON",
                ReceivingApplication = "UNKNOWN"
            };

            var prescriptionMessageResult = plugin.MessageFactory.CreatePrescription(prescription.ToDto(), messageOptions);
            if (prescriptionMessageResult.IsSuccess)
            {
                var serializationResult = prescriptionMessageResult.Value.Serialize();
                if (serializationResult.IsSuccess)
                {
                    return Result<string>.Success(serializationResult.Value);
                }
                else
                {
                    return Result<string>.Failure($"Failed to serialize prescription message: {serializationResult.Error}");
                }
            }
            else
            {
                return Result<string>.Failure($"Failed to create prescription message: {prescriptionMessageResult.Error}");
            }
        }
        
        // Fallback to human-readable for unsupported standards
        return Result<string>.Success($"Prescription: {prescription.Medication.DisplayName} for {prescription.Patient.Name.DisplayName}");
    }
    
    private async Task<Result<string>> GenerateEncounterMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (!encounterResult.IsSuccess)
            return Result<string>.Failure(encounterResult.Error);
            
        var encounter = encounterResult.Value;
        
        // Use plugin registry to find appropriate standard plugin
        var plugin = _pluginRegistry.GetPlugin(standard);
        if (plugin != null)
        {
            var messageOptions = new MessageOptions
            {
                Timestamp = DateTime.UtcNow,
                SendingApplication = "PIDGEON",
                ReceivingApplication = "UNKNOWN"
            };

            var admissionResult = plugin.MessageFactory.CreatePatientAdmission(encounter.Patient.ToDto(), messageOptions);
            if (admissionResult.IsSuccess)
            {
                var serializationResult = admissionResult.Value.Serialize();
                if (serializationResult.IsSuccess)
                {
                    return Result<string>.Success(serializationResult.Value);
                }
                else
                {
                    return Result<string>.Failure($"Failed to serialize encounter message: {serializationResult.Error}");
                }
            }
            else
            {
                return Result<string>.Failure($"Failed to create encounter message: {admissionResult.Error}");
            }
        }
        
        // Fallback to human-readable for unsupported standards
        return Result<string>.Success($"Encounter: {encounter.Patient.Name.DisplayName} at {encounter.Location} ({encounter.Type})");
    }
}