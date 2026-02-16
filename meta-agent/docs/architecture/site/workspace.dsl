workspace "meta-agent" "Governance and operating layer for AI-assisted software delivery" {

    model {
        !include model/10-elements.dsl
        !include model/meta-agent/software-system.dsl
        !include model/20-relationships.dsl
        !include model/deployment/30-local.dsl
        !include model/deployment/31-ci.dsl
    }

    views {
        !include views/10-system-context.dsl
        !include views/20-containers.dsl
        !include views/30-deployment-local.dsl
        !include views/31-deployment-ci.dsl

        theme default
    }

    !docs _docs
    !adrs adrs
}
