#include "command.h"
#include "command_handler.h"

discpp::Command::Command(const std::string& name) : name(name) {
	discpp::registered_commands.insert(std::make_pair(name, this));
}

discpp::Command::Command(const std::string& name, const std::string& desc, const std::vector<std::string>& hint_args, const std::function<void(discpp::Context)>& function, const std::vector<std::function<bool(discpp::Context)>>& requirements) : Command() {
	this->name = name;
	this->description = desc;
	this->hint_args = hint_args;
	this->function = function;
	this->requirements = requirements;

	discpp::registered_commands.insert(std::make_pair(name, this));
}

void discpp::Command::CommandBody(discpp::Context ctx) {
	if (function == nullptr) {
		std::cout << "Make sure to override the \"CommandBody(discpp::Context)\" method to get an actual command body for: \"" + name + "\"" << std::endl;
	} else {
		function(ctx);
	}
}

bool discpp::Command::CanRun(discpp::Context ctx) {
	bool requires = true;
	for (auto req_function : requirements) {
		requires = requires && req_function(ctx);
	}

	return requires;
}
