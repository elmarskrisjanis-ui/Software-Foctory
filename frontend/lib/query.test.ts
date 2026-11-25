import { describe, expect, it } from "vitest";
import { buildHref, isSafeExternalUrl, parseFilters, toApiQuery, STORE_HOSTS } from "./query";

describe("parseFilters", () => {
  it("keeps valid values", () => {
    expect(parseFilters({ group: "datortehnika", q: " asus ", minPrice: "100", sort: "price-asc", page: "2" })).toEqual({
      group: "datortehnika",
      q: "asus",
      minPrice: "100",
      sort: "price-asc",
      page: "2",
    });
  });

  it("drops malformed values", () => {
    expect(
      parseFilters({ category: "../../etc", store: "<script>", minPrice: "1e9", sort: "drop", page: "0", other: "x" }),
    ).toEqual({});
  });

  it("strips control characters and limits search length", () => {
    const f = parseFilters({ q: "a\u0000b" + "c".repeat(200) });
    expect(f.q?.startsWith("ab")).toBe(true);
    expect(f.q?.length).toBe(100);
  });

  it("uses the first value of repeated params", () => {
    expect(parseFilters({ store: ["m79", "tet"] })).toEqual({ store: "m79" });
  });
});

describe("buildHref", () => {
  it("resets page and group when category changes", () => {
    expect(buildHref({ group: "datortehnika", page: "3", sort: "price-asc" }, { category: "monitori" })).toBe(
      "/?category=monitori&sort=price-asc",
    );
  });

  it("clears category when group changes", () => {
    expect(buildHref({ category: "monitori" }, { group: "elektrotehnika" })).toBe("/?group=elektrotehnika");
  });

  it("keeps filters when paging", () => {
    expect(buildHref({ store: "tet" }, { page: "2" })).toBe("/?store=tet&page=2");
  });

  it("encodes search text", () => {
    expect(buildHref({}, { q: "a&b=c" })).toBe("/?q=a%26b%3Dc");
  });
});

describe("toApiQuery", () => {
  it("adds page size", () => {
    expect(toApiQuery({ store: "m79" }, 24)).toBe("store=m79&pageSize=24");
  });
});

describe("isSafeExternalUrl", () => {
  it("accepts https store links", () => {
    expect(isSafeExternalUrl("https://www.m79.lv/p/products/1", STORE_HOSTS)).toBe(true);
  });

  it.each(["javascript:alert(1)", "http://www.m79.lv/x", "https://evil.example/x", "https://a:b@www.m79.lv/", "not a url", null])(
    "rejects %s",
    (url) => {
      expect(isSafeExternalUrl(url, STORE_HOSTS)).toBe(false);
    },
  );
});
