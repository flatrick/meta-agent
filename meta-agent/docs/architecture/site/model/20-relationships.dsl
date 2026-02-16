developer -> cli "Runs commands and reviews outputs"
operator -> cli "Defines/updates governance policy"
automation -> cli "Invokes policy-gated autonomous flows"

cli -> core "Uses"
cli -> templates "Reads scaffold templates"
cli -> artifacts "Writes and reads runtime artifacts"
