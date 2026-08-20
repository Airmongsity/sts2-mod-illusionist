extends SceneTree

const TABLES := ["cards", "relics", "potions", "monsters", "powers", "events", "ancients"]

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 2:
		printerr("usage: extract_localization.gd <SlayTheSpire2.pck> <output-directory>")
		quit(2)
		return

	var pck_path := args[0]
	var output_dir := args[1]
	if not ProjectSettings.load_resource_pack(pck_path, true):
		printerr("unable to mount resource pack: " + pck_path)
		quit(3)
		return

	var mkdir_error := DirAccess.make_dir_recursive_absolute(output_dir)
	if mkdir_error != OK:
		printerr("unable to create localization output: %s (%s)" % [output_dir, mkdir_error])
		quit(4)
		return

	var missing: Array[String] = []
	for table in TABLES:
		var resource_path := "res://localization/eng/%s.json" % table
		if not FileAccess.file_exists(resource_path):
			missing.append(resource_path)
			continue
		var source := FileAccess.open(resource_path, FileAccess.READ)
		if source == null:
			missing.append(resource_path)
			continue
		var destination_path := output_dir.path_join("%s.json" % table)
		var destination := FileAccess.open(destination_path, FileAccess.WRITE)
		if destination == null:
			printerr("unable to write: " + destination_path)
			quit(5)
			return
		destination.store_string(source.get_as_text())

	var report := {
		"tables": TABLES,
		"missing": missing,
		"resourcePrefix": "res://localization/eng/"
	}
	var report_file := FileAccess.open(output_dir.path_join("pck-report.json"), FileAccess.WRITE)
	report_file.store_string(JSON.stringify(report, "  "))
	if not missing.is_empty():
		printerr("missing localization tables: " + ", ".join(missing))
		quit(6)
		return
	quit(0)
