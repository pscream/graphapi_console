### Step 3. Get messages from the C# application

Normally, it's required  to add a few package dependencies to the 'csproj' file:

```xml
  <ItemGroup>
    <PackageReference Include="Azure.Identity" Version="1.5.0" />
    <PackageReference Include="Microsoft.Graph" Version="4.15.0" />
    <PackageReference Include="Microsoft.Graph.Core" Version="2.0.7" />
  </ItemGroup>
```

Then, in the C# method you need to create a _Graph API client_, authenticate in Azure and request the API endpoints.

```csharp

// Create credentials
var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

// Create Graph API client
var graphServiceClient = new GraphServiceClient(clientSecretCredential);

// Perform API calls
var users = await client.Users.Request().Filter($"UserPrincipalName eq '{mailbox}'").GetAsync();
var folders = await client.Users[userId].MailFolders.Request().GetAsync();
var messages = await client.Users[fisrtUser.Id].MailFolders[folders.First().Id].Messages.Request().GetAsync();

```

All in all, the algorithm is like the following:

1. Register your app at the [Azure Portal](https://portal.azure.com/) in _Azure Active Directory_, in 'App registrations'
2. Create a client secret for your app in 'Certificates & secrets' under your app's registration. You can either create a password-like secret or add a certificate.
3. Ask and Administrator for the consent for required permissions. The app needs _'User.ReadWrite.All'_, _'MailboxSettings.ReadWrite'_ and _'Mail.ReadWrite'_ permissions.
4. Get all required credentials: tenantId, clientId and clientSecret and authenticate with them from the app. See above.
5. Perform API calls.
