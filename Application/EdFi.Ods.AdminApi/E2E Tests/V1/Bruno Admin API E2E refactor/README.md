# Admin API E2E Testing with Bruno

This repository contains end-to-end tests for the Admin API using Bruno test runner. 
The test suite supports both single-tenant and multi-tenant configurations with PostgreSQL and SQL Server databases.

## 🚀 Quick Start

### Prerequisites

- **Bruno CLI**: Install the Bruno command line interface
  ```bash
  npm install -g @usebruno/cli
  ```

- **Bruno Desktop (Optional)**: For running tests via UI
  - Download from: https://www.usebruno.com/downloads

- **Required tools**: `curl`, `jq`, `uuidgen`

### Authentication Setup

Before running tests, you need to generate an authentication token using the provided script:

```bash
sh get_token.sh
```

## 🧪 Running Tests

After generating the authentication token, run the E2E tests:

### Command Line Execution
```bash
bru run --env local --sandbox=developer --insecure -r v1/ --reporter-html results.html
```

### Command Options Explained
- `--env local`: Uses the local environment configuration
- `--sandbox=developer`: Enables developer sandbox mode
- `--insecure`: Ignores SSL certificate verification
- `-r v1/`: Runs tests from the v1 directory recursively
- `--reporter-html results.html`: Generates HTML test report

## 📁 Test Structure

The test suite is organized into the following modules:

- **Application**: Application lifecycle tests  
- **ClaimSets**: Claim set management tests
- **OdsInstances**: ODS instance management tests
- **User Management**: User administration tests
- **Tenants**: Tenant management tests
- **Vendors**: Vendor management tests

## 🔧 Environment Configuration

The `get_token.sh` script automatically configures the test environment by:

1. **Generating secure credentials**: Creates a unique client ID and compliant client secret
2. **Registering the client**: Registers the test client with the Admin API
3. **Obtaining access token**: Retrieves a valid JWT token for API authentication
4. **Updating environment variables**: Configures `environments/local.bru` with all necessary test variables

## 🛠️ Troubleshooting

### Common Issues

**SSL Certificate Errors**
- The script automatically sets `NODE_TLS_REJECT_UNAUTHORIZED=0` for development environments
- Use the `--insecure` flag when running Bruno tests

**Authentication Failures**
- Verify the API URL is accessible: `https://localhost/adminapi`
- Check that the Admin API service is running
- Ensure the database connection string is correct for your environment

**Token Expiration**
- Re-run the `get_token.sh` script to generate a fresh token
- Tokens are automatically configured with appropriate expiration times

### Debugging

Enable verbose output by modifying the script or checking:
- Registration response in the console output
- Token response length (should be substantial for valid JWT)
- Environment variables in `environments/local.bru`

## 📊 Test Reports

After test execution, review the generated HTML report (`results.html`) for:
- Test execution summary
- Individual test results
- Performance metrics
- Error details and stack traces

## 🔒 Security Considerations

- **Development Only**: This setup is intended for development and testing environments
- **SSL Verification**: Disabled for local testing convenience
- **Credentials**: Generated credentials are temporary and session-specific
- **Token Storage**: Tokens are stored in local environment files and should not be committed to version control
