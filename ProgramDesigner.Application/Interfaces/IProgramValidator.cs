using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Domain.Entities;

namespace ProgramDesigner.Application.Interfaces;

/// <summary>
/// Interface for program tree validation engine.
/// </summary>
public interface IProgramValidator
{
    ProgramValidationResult Validate(Node root);
}
