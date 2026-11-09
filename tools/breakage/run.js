// Breaks each behaviour on its own, in a copy of the repo, and checks that a test notices. A test that can't fail proves nothing.
//
//   node tools/breakage/run.js <copy> <list.js> <results.txt> <test filter> [test project]
//
// <copy> is a fresh `git clone` in a temp folder, never this repo: killing a run can leave a break in the source. A clone, not a
// robocopy, which keeps old timestamps, so that a build can skip the compile and test stale binaries. If a test needs something the
// clone lacks, it skips, so the baseline fails rather than call those breakages missed.
//
// [test project] is the copy's test project, relative to it. Left out, it's the one `*.Tests.csproj` under `tests/`, so the tool
// carries to another library unchanged (from Avonic's tools/breakage/run.js, which named its own).
//
// <list.js> exports the breakages, each [name, file, text to find, text to put there]:
//   module.exports = [['a hole goes unlisted', 'tests/Cornflower.Tests/Completeness/Holes.cs', 'found is ControlTheme', 'found is not null']];
//
// The unbroken copy has to pass first, having run some tests and skipped none. MISSED is a weak test or a rule that does nothing, so
// work out which. NO BUILD didn't test anything, as warnings are errors: rewrite it. NO RESULT and TIMED OUT mean the run itself failed,
// so nothing was shown, and a test that hangs takes this long to notice. Report the true numbers. It exits 1 unless all were caught.
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const [clone, list, out, filter, given] = process.argv.slice(2);
if (!clone || !list || !out || !filter) {
    console.error('Usage: node tools/breakage/run.js <copy> <list.js> <results.txt> <test filter> [test project]');
    process.exit(1);
}

const findProject = () => {
    const tests = path.join(clone, 'tests');
    const found = fs.existsSync(tests)
        ? fs.readdirSync(tests, { withFileTypes: true })
            .filter((entry) => entry.isDirectory())
            .flatMap((entry) => fs.readdirSync(path.join(tests, entry.name))
                .filter((name) => name.endsWith('.Tests.csproj'))
                .map((name) => `tests/${entry.name}/${name}`))
        : [];
    if (found.length !== 1) {
        console.error(`Found ${found.length} *.Tests.csproj under tests/ (${found.join(', ')}): name the test project.`);
        process.exit(1);
    }
    return found[0];
};
const project = given || findProject();

const breakages = require(path.resolve(list));
const TIMEOUT = 10 * 60 * 1000;

const run = (command) => {
    try {
        return { ok: true, text: execSync(command, { cwd: clone, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], maxBuffer: 1 << 28, timeout: TIMEOUT }) };
    } catch (error) {
        return { ok: false, timedOut: error.code === 'ETIMEDOUT', text: `${error.stdout || ''}${error.stderr || ''}` };
    }
};

const test = () => {
    const build = run(`dotnet build "${project}" --nologo -v q -m:8`);
    if (!build.ok) return { built: false, text: build.text };
    const result = run(`dotnet test "${project}" --nologo --no-build --filter "${filter}"`);
    return { built: true, passed: result.ok, timedOut: result.timedOut, text: result.text };
};

const log = (line) => {
    fs.appendFileSync(out, `${line}\n`);
    console.log(line);
};

fs.writeFileSync(out, '');
const baseline = test();
const counts = /Passed:\s+(\d+),\s+Skipped:\s+(\d+)/.exec(baseline.text);
const ran = counts ? Number(counts[1]) : 0;
const skipped = counts ? Number(counts[2]) : 0;
if (!baseline.built || !baseline.passed || ran === 0 || skipped > 0) {
    log(`baseline RED: ${ran} passed and ${skipped} skipped. It has to run some, and skip none, so check the filter.`);
    log(baseline.text.slice(-3000));
    process.exit(1);
}

log(`baseline green: ${ran} tests in ${project}`);

let caught = 0;
for (const [name, file, from, to] of breakages) {
    const full = path.join(clone, file);
    const original = fs.readFileSync(full, 'utf8');
    const text = original.replace(/\r\n/g, '\n');
    if (!text.includes(from)) {
        log(`NOT APPLIED  ${name}`);
        continue;
    }

    // A function, as `$'` in a replacement string means the rest of the file.
    fs.writeFileSync(full, text.replace(from, () => to));
    const result = test();
    fs.writeFileSync(full, original);
    if (!result.built) {
        log(`NO BUILD     ${name}: ${(/error [^\n]*/.exec(result.text) || [''])[0].slice(0, 140)}`);
    } else if (result.timedOut) {
        log(`TIMED OUT    ${name}`);
    } else if (result.passed) {
        log(`MISSED       ${name}`);
    } else {
        // Greedy to the last bracket, as a theory's arguments can hold their own.
        const failed = [...result.text.matchAll(/\.([A-Za-z0-9_]+)(\(.*\))? \[FAIL\]/g)].map((match) => match[1]);
        if (failed.length === 0) {
            log(`NO RESULT    ${name}: the run failed with no failing test`);
        } else {
            caught += 1;
            log(`caught       ${name}: ${[...new Set(failed)].slice(0, 2).join(', ')}`);
        }
    }
}

log(`${caught} of ${breakages.length} caught`);
if (caught < breakages.length) process.exit(1);
