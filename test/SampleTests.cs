namespace Readability.Tests
{
    using System.Text.Json;
    using Brackets;

    [TestClass]
    public class SampleTests
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [DataTestMethod]
        [DataRow("001"), DataRow("002"), DataRow("003-metadata-preferred"), DataRow("004-metadata-space-separated-properties"),
        DataRow("aclu"), DataRow("aktualne"), DataRow("archive-of-our-own"), DataRow("ars-1"), DataRow("base-url"), DataRow("base-url-base-element"),
        DataRow("base-url-base-element-relative"), DataRow("basic-tags-cleaning"), DataRow("bbc-1"), DataRow("blogger"), DataRow("breitbart"),
        DataRow("bug-1255978"), DataRow("buzzfeed-1"), DataRow("citylab-1"), DataRow("clean-links"), DataRow("cnet"), DataRow("cnet-svg-classes"),
        DataRow("cnn"), DataRow("comment-inside-script-parsing"), DataRow("daringfireball-1"), DataRow("data-url-image"), DataRow("dev418"),
        DataRow("dropbox-blog"), DataRow("ebb-org"), DataRow("ehow-1"), DataRow("ehow-2"), DataRow("embedded-videos"), DataRow("engadget"),
        DataRow("firefox-nightly-blog"), DataRow("folha"), DataRow("gmw"), DataRow("google-sre-book-1"), DataRow("guardian-1"), DataRow("heise"),
        DataRow("herald-sun-1"), DataRow("hidden-nodes"), DataRow("hukumusume"), DataRow("iab-1"), DataRow("ietf-1"), DataRow("js-link-replacement"),
        DataRow("keep-images"), DataRow("keep-tabular-data"), DataRow("la-nacion"), DataRow("lazy-image-1"), DataRow("lazy-image-2"), DataRow("lazy-image-3"),
        DataRow("lemonde-1"), DataRow("liberation-1"), DataRow("lifehacker-post-comment-load"), DataRow("lifehacker-working"), DataRow("links-in-tables"),
        DataRow("lwn-1"), DataRow("medicalnewstoday"), DataRow("medium-1"), DataRow("medium-2"), DataRow("medium-3"), DataRow("mercurial"),
        DataRow("metadata-content-missing"), DataRow("missing-paragraphs"), DataRow("mozilla-1"), DataRow("mozilla-2"), DataRow("msn"),
        DataRow("normalize-spaces"), DataRow("nytimes-1"), DataRow("nytimes-2"), DataRow("nytimes-3"), DataRow("nytimes-4"), DataRow("nytimes-5"),
        DataRow("pixnet"), DataRow("qq"), DataRow("quanta-1"), DataRow("remove-aria-hidden"), DataRow("remove-extra-brs"), DataRow("remove-extra-paragraphs"),
        DataRow("remove-script-tags"), DataRow("reordering-paragraphs"), DataRow("replace-brs"), DataRow("replace-font-tags"), DataRow("rtl-1"),
        DataRow("rtl-2"), DataRow("rtl-3"), DataRow("rtl-4"), DataRow("salon-1"), DataRow("seattletimes-1"), DataRow("simplyfound-1"),
        DataRow("social-buttons"), DataRow("style-tags-removal"), DataRow("svg-parsing"), DataRow("table-style-attributes"), DataRow("telegraph"),
        DataRow("theverge"), DataRow("title-and-h1-discrepancy"), DataRow("tmz-1"), DataRow("toc-missing"), DataRow("topicseed-1"), DataRow("tumblr"),
        DataRow("v8-blog"), DataRow("videos-1"), DataRow("videos-2"), DataRow("visibility-hidden"), DataRow("wapo-1"), DataRow("wapo-2"),
        DataRow("webmd-1"), DataRow("webmd-2"), DataRow("wikia"), DataRow("wikipedia"), DataRow("wikipedia-2"), DataRow("wikipedia-3"),
        DataRow("wordpress"), DataRow("yahoo-1"), DataRow("yahoo-2"), DataRow("yahoo-3"), DataRow("yahoo-4"), DataRow("youth")]
        public async Task Parse_SamplePage_AsExpected(string directory)
        {
            var path = Path.GetFullPath(Path.Combine(@"..\..\..\test-pages\", directory));
            Assert.IsTrue(Path.Exists(path), $"'{path}' doesn't exist");

            var sourceFileName = Path.Combine(path, "source.html");
            await using var sourceStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read);
            var sourceDocument = await Document.Html.ParseAsync(sourceStream, default);
            var reader = new DocumentReader(sourceDocument, new Uri("http://fakehost/"));
            var parsed = reader.Parse();
            Assert.IsNotNull(parsed);

            var metadataFileName = Path.Combine(path, "expected-metadata.json");
            await using var metadataStream = new FileStream(metadataFileName, FileMode.Open, FileAccess.Read);
            var expected = await JsonSerializer.DeserializeAsync<Article>(metadataStream, jsonOptions);
            Assert.IsNotNull(expected);
            var expectedFileName = Path.Combine(path, "expected.html");
            await using var expectedStream = new FileStream(expectedFileName, FileMode.Open, FileAccess.Read);
            var expectedContent = await Document.Html.ParseAsync(expectedStream, default);

            Assert.AreEqual(expected, parsed with { Content = null! });
            AssertAreEqual(expectedContent, parsed.Content as IEnumerable<Element>);
        }

        private static void AssertAreEqual(IEnumerable<Element>? expectedElements, IEnumerable<Element>? actualElements)
        {
            Assert.IsNotNull(expectedElements);
            Assert.IsNotNull(actualElements);

            var expectedEnumerator = expectedElements.GetEnumerator();
            var actualEnumerator = actualElements.GetEnumerator();

            while (actualEnumerator.MoveNext() && expectedEnumerator.MoveNext())
            {
                var expected = expectedEnumerator.Current;
                var actual = actualEnumerator.Current;

                var elemStr = $"{expected.GetType().Name}: {expected.Offset}";

                Assert.AreNotSame(expected, actual, elemStr);
                Assert.AreEqual(expected.Offset, actual.Offset, elemStr);
                Assert.AreEqual(expected.Length, actual.Length, elemStr);
                if (expected is CharacterData && actual is CharacterData)
                {
                    Assert.AreEqual(expected.ToString(), actual.ToString(), elemStr);
                }
                else if (expected is Tag expectedTag && actual is Tag actualTag)
                {
                    AssertAreEqual(expectedTag.EnumerateAttributes(), actualTag.EnumerateAttributes());

                    if (expectedTag is ParentTag expectedParent && actualTag is ParentTag actualParent)
                    {
                        AssertAreEqual(expectedParent, actualParent);
                    }
                }
                else if (expected is Attr expectedAttr && actual is Attr actualAttr)
                {
                    Assert.AreEqual(expectedAttr.HasValue, actualAttr.HasValue, elemStr);
                    Assert.AreEqual(expectedAttr.Name, actualAttr.Name, elemStr);
                    Assert.AreEqual(expectedAttr.ToString(), actualAttr.ToString(), elemStr);
                }
            }

            Assert.IsFalse(expectedEnumerator.MoveNext());
            Assert.IsFalse(actualEnumerator.MoveNext());
        }
    }
}