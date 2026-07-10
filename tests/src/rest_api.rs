use reqwest::StatusCode;
use reqwest::blocking::Client;
use serde::Deserialize;
use serde_json::json;
use rand::Rng;

use crate::util;

const CONFIG_TYPES: &[(&str, u8)] = &[
    ("Strategy", 1),
    ("Broker", 2),
    ("Fund", 3),
    ("Exchange", 4),
    ("Manager", 5),
];

#[derive(Deserialize)]
struct ConfigEntity {
    id: i32,
    code: String,
    #[allow(dead_code)]
    description: String,
    #[serde(rename = "type")]
    type_: u8,
}

#[derive(Deserialize)]
struct OrderResponse {
    id: i32,
}

struct Report {
    passed: usize,
    failed: usize,
}

impl Report {
    fn new() -> Self {
        Self { passed: 0, failed: 0 }
    }

    fn check(&mut self, name: &str, ok: bool, detail: impl std::fmt::Display) {
        if ok {
            self.passed += 1;
            println!("  PASS  {name} :: {detail}");
        } else {
            self.failed += 1;
            println!("  FAIL  {name} :: {detail}");
        }
    }
}

pub fn run(host: &str) -> Result<(), Box<dyn std::error::Error>> {
    let client = Client::new();
    let mut report = Report::new();

    let token = util::get_token_string(&client, format!("{host}/token"))?;
    println!("Token acquired.\n");

    println!("== ConfigurationController ==");
    test_configuration(&client, host, &token, &mut report)?;

    println!("\n== OrdersController ==");
    test_orders(&client, host, &token, &mut report)?;

    println!("\n{} passed, {} failed", report.passed, report.failed);
    if report.failed > 0 {
        return Err(format!("{} checks failed", report.failed).into());
    }
    Ok(())
}

fn test_configuration(
    client: &Client,
    host: &str,
    token: &str,
    report: &mut Report,
) -> Result<(), Box<dyn std::error::Error>> {
    let base = format!("{host}/api/configuration");
    let suffix: u32 = rand::rng().random_range(0..10_000);

    let mut created: Vec<(u8, i32, String)> = Vec::new();

    for (name, type_id) in CONFIG_TYPES {
        let code = format!("T-{name}-{suffix}");
        let body = json!({
            "code": code,
            "description": format!("Test {name} {suffix}"),
            "type": type_id,
        });

        let resp = client.post(&base).bearer_auth(token).json(&body).send()?;
        let status = resp.status();
        let text = resp.text().unwrap_or_default();

        match (status.is_success(), text.trim().parse::<i32>()) {
            (true, Ok(id)) => {
                created.push((*type_id, id, code));
                report.check(&format!("POST {name}"), true, format!("HTTP {} id={id}", status.as_u16()));
            }
            (true, Err(_)) => {
                report.check(&format!("POST {name}"), false, format!("HTTP {} bad id body '{text}'", status.as_u16()));
            }
            (false, _) => {
                report.check(&format!("POST {name}"), false, format!("HTTP {} {text}", status.as_u16()));
            }
        }
    }

    for (type_id, id, code) in &created {
        let name = type_name(*type_id);
        let resp = client.get(format!("{base}/{id}")).bearer_auth(token).query(&[("type", name)]).send()?;
        let status = resp.status();
        if status.is_success() {
            let e: ConfigEntity = resp.json()?;
            let ok = e.id == *id && &e.code == code && e.type_ == *type_id;
            report.check(&format!("GET {name}/{id}"), ok, format!("code={} type={}", e.code, e.type_));
        } else {
            report.check(&format!("GET {name}/{id}"), false, format!("HTTP {}", status.as_u16()));
        }
    }

    let query: Vec<(&str, &str)> = CONFIG_TYPES.iter().map(|(n, _)| ("types", *n)).collect();
    let resp = client.get(&base).bearer_auth(token).query(&query).send()?;
    let status = resp.status();
    if status.is_success() {
        let all: Vec<ConfigEntity> = resp.json()?;
        let all_found = created
            .iter()
            .all(|(t, id, _)| all.iter().any(|e| e.id == *id && e.type_ == *t));
        report.check("GET all (filtered)", all_found, format!("returned {} entities", all.len()));
    } else {
        report.check("GET all (filtered)", false, format!("HTTP {}", status.as_u16()));
    }

    let resp = client.get(&base).bearer_auth(token).query(&[("types", "Broker")]).send()?;
    if resp.status().is_success() {
        let brokers: Vec<ConfigEntity> = resp.json()?;
        let only = brokers.iter().all(|e| e.type_ == 2);
        report.check("GET types=Broker", only, format!("{} entities, all broker={only}", brokers.len()));
    } else {
        report.check("GET types=Broker", false, format!("HTTP {}", resp.status().as_u16()));
    }

    let resp = client
        .post(&base)
        .bearer_auth(token)
        .json(&json!({ "code": "BAD", "description": "x", "type": 0 }))
        .send()?;
    report.check("POST type=Unspecified -> 400", resp.status() == StatusCode::BAD_REQUEST, format!("HTTP {}", resp.status().as_u16()));

    let resp = client.get(format!("{base}/999999")).bearer_auth(token).query(&[("type", "Broker")]).send()?;
    report.check("GET missing id -> 404", resp.status() == StatusCode::NOT_FOUND, format!("HTTP {}", resp.status().as_u16()));

    Ok(())
}

fn test_orders(
    client: &Client,
    host: &str,
    token: &str,
    report: &mut Report,
) -> Result<(), Box<dyn std::error::Error>> {
    let base = format!("{host}/api/orders");

    let resp = client.get(&base).bearer_auth(token).send()?;
    let status = resp.status();
    if !status.is_success() {
        report.check("GET orders", false, format!("HTTP {}", status.as_u16()));
        return Ok(());
    }

    let orders: Vec<OrderResponse> = resp.json()?;
    report.check("GET orders", true, format!("{} orders", orders.len()));

    match orders.first() {
        Some(first) => {
            let resp = client.get(format!("{base}/{}", first.id)).bearer_auth(token).send()?;
            report.check(&format!("GET orders/{}", first.id), resp.status().is_success(), format!("HTTP {}", resp.status().as_u16()));
        }
        None => println!("  (no orders present — run END_TO_END first to exercise GET-by-id)"),
    }

    let resp = client.get(format!("{base}/999999")).bearer_auth(token).send()?;
    report.check("GET missing order -> 404", resp.status() == StatusCode::NOT_FOUND, format!("HTTP {}", resp.status().as_u16()));

    Ok(())
}

fn type_name(type_id: u8) -> &'static str {
    CONFIG_TYPES
        .iter()
        .find(|(_, id)| *id == type_id)
        .map(|(n, _)| *n)
        .unwrap_or("Unspecified")
}