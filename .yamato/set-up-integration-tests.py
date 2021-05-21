import os
import shutil
import glob
from collections import OrderedDict
from unityparser import UnityDocument

# NOTE: This must match the flag defined in the Unity Integration Tests
_INTEGRATION_TEST_DEFINE = "INTEGRATION_TEST"

# This script is executed inside of a bokken image in order to automate the manual steps a user would
# perform when going through the tutorials. This allows us to perform integration tests on the expected final
# state of a tutorial project.


script_dir = os.path.dirname(os.path.realpath(__file__))
root_dir = os.path.join(script_dir, "..", "tutorials", "pick_and_place")
external_scripts_dir = os.path.join(root_dir, "Scripts")
project_dir = os.path.join(root_dir, "PickAndPlaceProject")
project_scripts_dir = os.path.join(project_dir, "Assets", "Scripts")
project_settings_file = os.path.join(project_dir, "ProjectSettings", "ProjectSettings.asset")

scripts_to_move = glob.glob(os.path.join(external_scripts_dir, "*.cs"))
for external_script in scripts_to_move:
    script_name = os.path.basename(external_script)
    shutil.copyfile(external_script, os.path.join(project_scripts_dir, script_name))

project_settings_asset = UnityDocument.load_yaml(project_settings_file)
scripting_defines = project_settings_asset.entry.scriptingDefineSymbols  # type: OrderedDict
if scripting_defines[1]:
    scripting_defines[1] += f";{_INTEGRATION_TEST_DEFINE}"
else:
    scripting_defines[1] = _INTEGRATION_TEST_DEFINE
project_settings_asset.dump_yaml()
