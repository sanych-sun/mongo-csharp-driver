import os
import yaml
import requests
import re
import sys

jira_base_url = os.getenv('JIRA_URL', 'https://jira.mongodb.org/')
version = sys.argv[1]
configPath = sys.argv[2]

common_parameters = {"version": version}


def load_config(config_path):
    print("Loading config...")
    with open(config_path, 'r') as stream:
        try:
            return yaml.safe_load(stream)
        except yaml.YAMLError as e:
            print('Cannot load config file:', e)
    print("Config loaded.")


def get_field(source, path):
    elements = path.split('.')
    for elem in elements:
        source = source[elem]
    return source


def apply_template(template, parameters):
    return re.sub(r'\$\{([\w.]+)}', lambda m: get_field(parameters, m.group(1)), template)


def process_query_section(section):
    search_url = jira_base_url + 'rest/api/2/search'
    result = ""
    loaded_count = 0
    total_count = 1  # do not know yet, just set to any not-zero value to enter the loop
    fields = section["fields"]
    jql = config["base-filter"]
    section_filter = section.get("filter","")
    if section_filter != "":
        jql = jql + " AND (" + section["filter"] + ")"
    jql = apply_template(jql, common_parameters)

    while total_count > loaded_count:
        r = requests.post(search_url, json={"fields": fields,  "jql": jql,  "maxResults": 10,  "startAt": loaded_count})

        data = r.json()
        total_count = data["total"]
        for issue in data["issues"]:
            loaded_count += 1
            issue_params = common_parameters.copy()
            issue_params["key"] = issue["key"]
            for field in section["fields"]:
                issue_params[field] = issue["fields"][field]

            result += "" if result == "" else "\n"
            result += apply_template(section["template"], issue_params)

    if result != "":
        title = section.get("title", "")
        if title != "":
            result = apply_template(title, common_parameters) + '\n' + result

    return result


def process_sections(section):
    if type(section) is str:
        return apply_template(section, common_parameters)
    elif section is None:
        return ""
    else:
        return process_query_section(section)


def publish_release_notes(title, tag, content):
    github_base_url = 'https://api.github.com/repos/'
    repo = 'sanych-sun/mongo-csharp-driver'
    github_api_key = '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'

    print("Publishing release notes...")
    url = '{base}{repo}/releases'.format(base=github_base_url, repo=repo)
    headers = {
        "Authorization": "Bearer {api_key}".format(api_key=github_api_key),
        "X-GitHub-Api-Version": "2022-11-28",
        "Accept": "application/vnd.github+json"
    }
    r = requests.get("{url}/tags/{tag}".format(url=url, tag=tag), headers=headers)
    if r.status_code == 200:
        raise SystemExit("Release with the tag already exists")

    post_data = {
        "tag_name": tag,
        "name": title,
        "body": content,
        "draft": True,
        "generate_release_notes": False,
        "make_latest": "false"
    }
    r = requests.post(url, json=post_data, headers=headers)
    if not r.ok:
        raise SystemExit("Failed to create the release notes: ({code}) {reason}". format(code=r.status_code, reason=r.reason))


config = load_config(configPath)

print("Processing title...")
release_title = apply_template(config["title"], common_parameters)
print("Title: {title}".format(title = release_title))
print("Processing tag...")
release_tag = apply_template(config["tag"], common_parameters)
print("Tag: {tag}".format(tag = release_tag))

print("Processing content...")
release_notes = ""
for value in config["sections"]:
    section_content = process_sections(value)
    if section_content != "":
        release_notes += "" if release_notes == "" else "\n\n"
        release_notes += section_content
print("----------")
print(release_notes)
print("----------")

publish_release_notes(release_title, release_tag, release_notes)

print("Done.")
