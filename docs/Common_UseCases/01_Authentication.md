# Authentication

## Log in with username and password

```bash
jf auth login --server https://your-server.example.com --username admin
```

You will be prompted for your password. Credentials are saved locally for future commands.

## Log in with an API key

```bash
jf auth login --server https://your-server.example.com --api-key YOUR_KEY
```

## Check who you are logged in as

```bash
jf auth whoami
```

Example output:

```
Field         | Value
--------------+-------------------------------------
Name          | testuser
ID            | 3ab506e8-7d40-416e-83a0-f501ad76ba51
Administrator | Yes
Server        | https://jf.example.com
```

## Log out

```bash
jf auth logout
```

## Quick Connect login

If your server has Quick Connect enabled, you can authenticate by approving a code on an already-signed-in device:

```bash
# Check if Quick Connect is available
jf auth quick status --server https://your-server.example.com

# Start a Quick Connect login flow (displays a code to approve)
jf auth quick login --server https://your-server.example.com

# Approve a pending Quick Connect code (from an authenticated session)
jf auth quick approve --code 123456
```

## Manage API keys (admin)

```bash
# List existing API keys
jf auth keys list

# Create a new API key
jf auth keys create --app "My Script"

# Revoke an API key
jf auth keys delete --key YOUR_KEY
```
