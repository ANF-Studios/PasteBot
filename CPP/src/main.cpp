#include <discpp/client.h>
#include <discpp/context.h>
#include <discpp/command_handler.h>
#include <discpp/client_config.h>

int main(int argc, const char* argv[]) {
	auto* config = new discpp::ClientConfig({"pb"});
	discpp::Client bot{ "TOKEN_HERE", config };

	discpp::Command ping_command("ping", "Quick example of a command", {}, [](discpp::Context ctx) {
		ctx.Send("pong!");
	}, {});

	return bot.Run();
}
