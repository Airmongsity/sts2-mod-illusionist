extends SceneTree

# Packs all mod resources under res://illusionist/ into illusionist.pck.
# The game expects modded loc tables at res://<mod_id>/localization/<lang>/<file>,
# so the in-pck paths must mirror the project layout exactly.

const OUTPUT_DIR := "res://build"
const OUTPUT_FILE := "res://build/illusionist.pck"
const CONTENT_DIRS := ["res://illusionist"]
const SKIP_EXTENSIONS := [".import", ".uid"]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var packer := PCKPacker.new()
	var ok := packer.pck_start(OUTPUT_FILE)
	if ok != OK:
		push_error("pck_start failed: %s" % ok)
		quit(1)
		return

	var added := 0
	for dir in CONTENT_DIRS:
		added += _add_dir_recursive(packer, dir)

	if added == 0:
		push_error("No files were added to the PCK. Check CONTENT_DIRS paths.")
		quit(1)
		return

	var flush_ok := packer.flush()
	if flush_ok != OK:
		push_error("flush failed: %s" % flush_ok)
		quit(1)
		return

	print("PCK built: %s (%d files)" % [OUTPUT_FILE, added])
	quit(0)

func _add_dir_recursive(packer: PCKPacker, dir_path: String) -> int:
	var count := 0
	var dir := DirAccess.open(dir_path)
	if dir == null:
		push_warning("Could not open dir: %s" % dir_path)
		return 0
	dir.list_dir_begin()
	var name := dir.get_next()
	while name != "":
		if name == "." or name == "..":
			name = dir.get_next()
			continue
		var full := dir_path.path_join(name)
		if dir.current_is_dir():
			count += _add_dir_recursive(packer, full)
		else:
			var skip := false
			for ext in SKIP_EXTENSIONS:
				if full.ends_with(ext):
					skip = true
					break
			if not skip:
				var add_ok := packer.add_file(full, full)
				if add_ok != OK:
					push_error("add_file failed: %s %s" % [full, add_ok])
					quit(1)
				else:
					count += 1
		name = dir.get_next()
	dir.list_dir_end()
	return count
