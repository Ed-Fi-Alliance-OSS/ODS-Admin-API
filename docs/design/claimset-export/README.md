# Summary

Conduct a spike to evaluate the feasibility and impact of updating the claim set export endpoint in Admin API v3. The goal is to align it with the descriptor-only format used by CMS and provide recommendations or implementation as needed.

## Context

The current claim set export endpoint in Admin API v3 returns the full claim tree, while CMS only returns a descriptor. This discrepancy needs to be addressed.

## Acceptance criteria

- Determine the feasibility of updating the claim set export format.
- Assess the impact of the proposed changes.
- Provide recommendations or implementation steps based on the findings.
- Reference migration utility as needed.
- Assess the impact of any change on Admin App.
- Assess the impact of  Name field - should not accept blank character. 

## Other information

- Gap Analysis row reference:
    - Row 11 – “Claim set export”
        - GAP: CMS returns only a descriptor; Admin API returns the full claim tree.
        - Decision: Admin API /v3 – Spike to determine feasibility of new export format (VM to provide reference to migration utility)

- Claim set export

    Admin API emits the full resource-claim tree (actions, default strategies, overrides). CMS returns only a high-level claim set descriptor.

    Action: `GET /v2/claimSets/{{id}}/export`

    Response Payload (CMS)
    ```
    {
    "id": 5,
    "name": "EdFiSandbox",
    "_isSystemReserved": true,
    "_applications": {}
    }
    ```

    Response Payload (Admin API)
    ```
    {
    "resourceClaims": [
        {
        "id": 1,
        "name": "types",
        "actions": [
            {
            "name": "Read",
            "enabled": true
            }
        ],
        "_defaultAuthorizationStrategiesForCRUD": [
            {
            "actionId": 2,
            "actionName": "Read",
            "authorizationStrategies": [
                {
                "authStrategyId": 1,
                "authStrategyName": "NoFurtherAuthorizationRequired",
                "isInheritedFromParent": false
                }
            ]
            }
        ],
        "authorizationStrategyOverridesForCRUD": [],
        "children": []
        }
    ]
    }
    ```

---

# Developer findings

- One of the reason the endpoint is done like that is the ability to import the claimset into adminapi, setting different values in the claimset properties giving the user the right template to import it again with different characteristcs.
- This also applies in Admin App where user export the claimset, the user edits it manually and then imports it again. This process is doing by using the Admin API endpoints.
- If we are going to only export the descriptor, it means the user needs to understand the how to override the crud operations or other property structure, probably a good documentation might help with this.
- The impact in Admin App depends on Admin API changes, since the content is exported as txt file with the JSON payload from Admin API. The import uses the file content to parse it into a JSON payload that will be used in the Admin API import endpoint.

# Plan (Using Superpowers)

- [Spike Design](claimset-export-spike-design.md)
- [Option (A) Design](claimset-export-spike-design.md)

