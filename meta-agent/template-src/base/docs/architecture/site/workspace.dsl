workspace "{{ project_name }}" "Architecture model for {{ project_name }}" {

    model {
        !include model/10-elements.dsl
        !include model/system/software-system.dsl
        !include model/20-relationships.dsl
        !include model/deployment/30-local.dsl
        !include model/deployment/31-production.dsl
    }

    views {
        !include views/10-system-context.dsl
        !include views/20-containers.dsl
        !include views/30-deployment-local.dsl
        !include views/31-deployment-production.dsl

        theme default
    }

    !docs _docs
    !adrs adrs
}
