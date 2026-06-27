use serde_json::json;
use std::thread;
use std::time::Duration;

mod util;
mod fix_generator;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let host = util::get_host_url();
    let count = util::get_number_of_items_to_post();

    let token_url = format!("http://{host}/token");
    let fix_url = format!("http://{host}/fix");

    let client = reqwest::blocking::Client::new();

    let token = util::get_token_str(&client, token_url);
    println!("Token successfully received: {token}");

    println!("Firing {count} FIX messages at {fix_url} (1s interval)...\n");

    for seq in 1..=count {
        let msg = fix_generator::compose_fix(seq);

        match client.post(&fix_url)
                .bearer_auth(&token)
                .json(&json!({"message": msg}))
                .send() {
            Ok(response) => {
                let status = response.status();
                let body = response.text().unwrap_or_default();
                println!(
                    "[{seq}/{count}] -> HTTP {} {body}",
                    status.as_u16()
                );
            }
            Err(e) => {
                eprintln!("[{seq}/{count}] -> request failed: {e}");
            }
        }

        if seq < count {
            thread::sleep(Duration::from_secs(1));
        }
    }

    println!("Done.");

    Ok(())
}
