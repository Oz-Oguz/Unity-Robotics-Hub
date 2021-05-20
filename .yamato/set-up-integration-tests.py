import os
import shutil
import glob
import yaml

# This script is executed inside of a bokken image in order to automate the manual steps a user would
# perform when going through the tutorials. This allows us to perform integration tests on the expected final
# state of a tutorial project.

script_dir = os.path.dirname(os.path.realpath(__file__))
root = os.path.join(script_dir, "..", "tutorials", "pick_and_place")
external_scripts = os.path.join(root, "Scripts")
project = os.path.join(root, "PickAndPlaceProject")
project_scripts = os.path.join(project, "Assets", "Scripts")
project_settings = os.path.join(project, "ProjectSettings", "ProjectSettings.asset")

scripts_to_move = glob.glob(os.path.join(external_scripts, "*.cs"))
for external_script in scripts_to_move:
    script_name = os.path.basename(external_script)
    shutil.copyfile(external_script, os.path.join(project_scripts, script_name))

with open(project_settings, "r") as f:
    settings_yaml = yaml.full_load(f)

settings_yaml["PlayerSettings"]["scriptingDefineSymbols"].append(["INTEGRATION_TESTS"])

with open(project_settings, "w") as f:
    yaml.dump(settings_yaml, f)

