# API Coding Standards

The below document reflects the current coding standards for the backend. We should try and follow these as much as possible. If there is ever a time we need to deviate from this document, we should meet as a team to try and agree on the right approach.

## Try to align with REST (to a certain degree)
- Use the correct verb:
	- `GET /collection/` - Used for searching or just retrieving a list of objects
	- `GET /collection/{id}` - Used for retrieving details about a specific object in the collection
	- `POST /collection` - Create a new object in the collection
	- `PUT (or PATCH) /collection/{id}` - Update a specific object in the collection. Depending on the business requirements for this, we may want to update the complete object (so a `PUT`), or just a part of the object (a `PATCH`).
	- `DELETE /collection/{id}` - Remove a specific object from the collection
- If it makes sense to access a collection only through a member of a different collection, we can add that collection after the `{id}` parameter on the parent collection; for example, `GET /collection/{id}/subcollection`.

If it seems that we need to deviate from REST, then we should validate this with other members of the team to ensure there isn't a better way to do so with REST.

## DTOs
- Naming: Name it according to the endpoint (self-documenting), suffix it with `Dto` (e.g. `ProductDto`). If there are different ones for request/response, then suffix them with `RequestDto` or `ResponseDto` (e.g. `ProductRequestDto` or `ProductResponseDto`).
- Placement: In the Interface project that corresponds with the relevant domain.

## Control Flow
We are using the Result pattern in this app. In general, this means the following:

- Service methods should return either a `Result` or a `Result<T>` type.
- Any expected errors (for example, an invalid `UserID` passed in as a filter) should be handled by returning an error `Result`
- If everything happens as it should (the happy path), a success `Result` should be returned, possibly with results inside (depending on if you are returning the `Result<T>` type).
  
## Naming of projects
- Each part of the domain has an interface and a service project (e.g. `Products.Interface` and `Products.Service`)
- The interface projects contain any interfaces needed to consume that part of the domain, and any DTOs that are needed while interfacing with that domain.
- The service projects contain concrete implementations of the interfaces, along with any other business/domain logic. Should include FluentValidator validation.

## General Coding Guidelines
- Follow the [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) where applicable.
- Use a lower case d when creating an variable with "Id" in it. Example, userId, leadId.
- Use 'ct' for the variable name when creating a CancellationToken.
- Use curly braces for all control flow statements (if, else, for, while, etc.) even if they are one-liners.

## Plural vs. Singular Naming
The following objects should be plural:

- DB Tables
- Controllers
- Services
- Endpoints

The following objects should be singular:

- DTOs
- Entities

## Database Migrations
We need to handle these differently, and there are ongoing discussions about how this should be handled. Options that have been discussed are:

- EF Code First Migrations
- FluentMigrator
- What we are doing now

One thing that should be noted while this is figured out is that any changes to the database should happen in a new migration file. We should never go back and change migration files already pushed to version control or applied to a database.
